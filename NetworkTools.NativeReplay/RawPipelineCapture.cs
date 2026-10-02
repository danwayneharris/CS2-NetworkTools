using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Net;
using NativeReplay;
using Unity.Entities;
using Unity.Mathematics;

static class RawPipelineCapture {
    static JsonElement Read(string path) { using var d=JsonDocument.Parse(File.ReadAllText(path));return d.RootElement.Clone(); }
    static Entity[] Roots(JsonElement capture) => capture.GetProperty("roots").EnumerateArray().Select(RawEdgeCapture.Id).ToArray();
    static string Job(JsonElement capture) => capture.GetProperty("job").GetString()!.Split('+')[1];
    static object Get(ReplayWorld world, Entity entity, Type type) => typeof(ReplayWorld).GetMethod("Get")!.MakeGenericMethod(type).Invoke(world,new object[]{entity})!;

    public static int Run(string tracePath,string output) {
        if(File.Exists(output))throw new IOException("Refusing to overwrite evidence");
        var native=typeof(GeometrySystem).Assembly;
        if(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(native.Location)))!=RawEdgeCapture.GameHash)throw new ArgumentException("Game hash changed");
        var status=Read(tracePath).GetProperty("result");
        if(status.GetProperty("fault").ValueKind!=JsonValueKind.Null || status.GetProperty("insideCapturedPass").GetBoolean()
            || status.GetProperty("remainingPasses").GetInt32()!=0 || status.GetProperty("gameSha256").GetString()!=RawEdgeCapture.GameHash)
            throw new ArgumentException("Incomplete trace");
        var paths=status.GetProperty("events").EnumerateArray().Where(e=>e.TryGetProperty("path",out _)).Select(e=>e.GetProperty("path").GetString()!).ToArray();
        var captures=paths.Select(Read).Where(c=>Job(c)!="CalculateNodeGeometryJob").ToArray();
        foreach(var c in captures) {
            if(c.GetProperty("schemaVersion").GetInt32()!=1 || c.GetProperty("gameModuleVersionId").GetString()!=native.ManifestModule.ModuleVersionId.ToString()
                || c.GetProperty("operationId").GetString()!=status.GetProperty("operationId").GetString()
                || c.GetProperty("citySession").GetString()!=status.GetProperty("citySession").GetString()
                || c.GetProperty("pass").GetInt32()!=1)throw new ArgumentException("Capture identity/schema mismatch");
            var errors=c.GetProperty("errors").EnumerateArray().Select(e=>e.GetString()).ToArray();
            if(errors.Any(e=>!(Job(c)=="FlattenNodeGeometryJob" && e=="job_field:m_EdgeHeightMap")))throw new ArgumentException("Uncaptured stage dependency");
        }
        JsonElement[] Stage(string name,string phase)=>captures.Where(c=>Job(c)==name+"Job" && c.GetProperty("phase").GetString()==phase).ToArray();
        var init=Stage("InitializeNodeGeometry","entry");var edge=Stage("CalculateEdgeGeometry","entry").Single();
        var flatten=Stage("FlattenNodeGeometry","entry");var finish=Stage("FinishEdgeGeometry","entry").Single();
        var initialNodes=init.SelectMany(Roots).ToHashSet();var edgeIds=Roots(edge);var edgeSet=edgeIds.ToHashSet();
        if(initialNodes.Count==0 || edgeIds.Length==0)throw new ArgumentException("Empty pipeline");
        foreach(string stage in new[]{"InitializeNodeGeometry","CalculateEdgeGeometry","FlattenNodeGeometry","FinishEdgeGeometry"}) {
            var before=Stage(stage,"entry").SelectMany(Roots).ToArray();var after=Stage(stage,"exit").SelectMany(Roots).ToArray();
            if(!before.SequenceEqual(after) || before.Distinct().Count()!=before.Length)throw new ArgumentException("Stage membership changed: "+stage);
        }
        var cells=new Dictionary<(Entity,Type),JsonElement>();
        void Add(Entity entity,Type type,JsonElement cell,int stage) {
            // Never import outputs of an earlier computed stage, even if absent in
            // an incomplete starting capture. A subsequent read then fails closed.
            if(stage>0 && type==typeof(NodeGeometry) && initialNodes.Contains(entity))return;
            if(stage>1 && edgeSet.Contains(entity) && (type==typeof(EdgeGeometry)||type==typeof(StartNodeGeometry)||type==typeof(EndNodeGeometry)))return;
            var key=(entity,type);
            if(cells.TryGetValue(key,out var existing)) {
                if(existing.GetProperty("presence").GetString()!=cell.GetProperty("presence").GetString()
                    || (cell.GetProperty("presence").GetString()=="present" && !JsonNode.DeepEquals(JsonNode.Parse(existing.GetProperty("value").GetRawText()),JsonNode.Parse(cell.GetProperty("value").GetRawText()))))
                    throw new ArgumentException("Unexplained upstream mutation: "+entity+"/"+type.Name);
            } else cells.Add(key,cell);
        }
        void Import(JsonElement capture,int stage) {
            foreach(var (entity,row) in RawEdgeCapture.Rows(capture))foreach(var cell in row.EnumerateObject()) {
                var type=native.GetType(cell.Name,true)!;
                if(cell.Value.TryGetProperty("buffer",out var b)&&b.GetBoolean())type=type.MakeArrayType();
                Add(entity,type,cell.Value,stage);
            }
            var roots=Roots(capture);
            foreach(var field in capture.GetProperty("fields").EnumerateObject()) {
                var f=field.Value;
                if(f.ValueKind!=JsonValueKind.Object || !f.TryGetProperty("component",out var component) || !f.TryGetProperty("presence",out var presence))continue;
                var type=native.GetType(component.GetString()!,true)!;
                for(int i=0;i<roots.Length;i++) {
                    var cell=JsonSerializer.SerializeToElement(new{presence=presence.GetString(),value=presence.GetString()=="present"?f.GetProperty("values")[i]:(JsonElement?)null});
                    Add(roots[i],type,cell,stage);
                }
            }
        }
        foreach(var c in init)Import(c,0);Import(edge,1);foreach(var c in flatten)Import(c,2);Import(finish,3);
        var world=new ReplayWorld();
        foreach(var (key,cell) in cells) {
            string? presence=cell.GetProperty("presence").GetString();
            if(presence=="absent")typeof(ReplayWorld).GetMethod("Absent")!.MakeGenericMethod(key.Item2).Invoke(world,new object[]{key.Item1});
            else if(presence=="present")typeof(ReplayWorld).GetMethod("Record")!.MakeGenericMethod(key.Item2).Invoke(world,new[]{(object)key.Item1,RawEdgeCapture.Decode(key.Item2,cell.GetProperty("value"),key.ToString())});
            else throw new ArgumentException("Unknown input: "+key);
        }
        var reports=new List<object>();
        void Report(string name,Type[] types,ReplayMap<int2,float4>? map=null) {
            var diffs=new List<object>();int count=0;
            foreach(var expected in Stage(name,"exit")) {
                var rows=RawEdgeCapture.Rows(expected);
                foreach(var entity in Roots(expected))foreach(var type in types) {
                    JsonElement value;
                    if(rows[entity].TryGetProperty(type.FullName!,out var cell))value=cell.GetProperty("value");
                    else {
                        var field=expected.GetProperty("fields").EnumerateObject().Select(p=>p.Value).Single(f=>f.ValueKind==JsonValueKind.Object&&f.TryGetProperty("component",out var component)&&component.GetString()==type.FullName&&f.TryGetProperty("values",out _));
                        value=field.GetProperty("values")[Array.IndexOf(Roots(expected),entity)];
                    }
                    RawEdgeCapture.Compare(Get(world,entity,type),value,entity+"/"+type.Name,diffs,ref count);
                }
            }
            if(map!=null) {
                var expected=finish.GetProperty("fields").GetProperty("m_EdgeHeightMap").GetProperty("entries").EnumerateArray().ToArray();
                if(expected.Length!=map.Values.Count)throw new ArgumentException("Height-map key count mismatch");
                foreach(var row in expected) {
                    var key=new int2(row.GetProperty("key")[0].GetInt32(),row.GetProperty("key")[1].GetInt32());
                    if(!map.Values.TryGetValue(key,out var value))throw new ArgumentException("Height-map key missing");
                    for(int i=0;i<4;i++)RawEdgeCapture.Compare(value[i],row.GetProperty("value")[i],key+"/HeightMap/"+i,diffs,ref count);
                }
            }
            var encoded=JsonSerializer.SerializeToElement(diffs);double maximum=0;bool accepted=true;
            var controlErrors=new Dictionary<string,double>();
            foreach(var d in encoded.EnumerateArray()) {
                string path=d.GetProperty("path").GetString()!;
                bool spatial=path.Contains("/EdgeGeometry/")||path.Contains("/StartNodeGeometry/")||path.Contains("/EndNodeGeometry/")||path.Contains("/HeightMap/")||path.Contains("/NodeGeometry/m_Position")||path.Contains("/NodeGeometry/m_Offset");
                if(d.GetProperty("native").ValueKind!=JsonValueKind.Number||!spatial){accepted=false;continue;}
                double delta=Math.Abs(d.GetProperty("native").GetDouble()-d.GetProperty("replay").GetDouble());maximum=Math.Max(maximum,delta);
                if(System.Text.RegularExpressions.Regex.IsMatch(path,@"/(m_Left|m_Right)/[abcd]/[xyz]$")) {
                    string control=path.Substring(0,path.Length-2);
                    controlErrors[control]=controlErrors.GetValueOrDefault(control)+delta*delta;
                }
                if(delta>0.03)accepted=false;
            }
            double controlMaximum=controlErrors.Count==0?0:Math.Sqrt(controlErrors.Values.Max());
            if(controlMaximum>0.03)accepted=false;
            reports.Add(new{stage=name,comparedScalarFields=count,exact=diffs.Count==0,withinResearchTolerance=accepted,maxSpatialComponentErrorMetres=maximum,maxCurveControlPointErrorMetres=controlMaximum,differences=diffs});
        }
        foreach(var capture in init)foreach(var entity in Roots(capture)) {
            var job=WorldStageTests.Bind<InitializeNodeGeometryJob>(world);job.m_Loaded=capture.GetProperty("fields").GetProperty("m_Loaded").GetBoolean();job.Execute(new ReplayChunk(world,new[]{entity}));
        }
        Report("InitializeNodeGeometry",new[]{typeof(NodeGeometry)});
        var calculate=WorldStageTests.Bind<CalculateEdgeGeometryJob>(world);
        calculate.m_Entities=new(i=>edgeIds[i],(_,_)=>throw new InvalidOperationException("Read-only identity"),edgeIds.Length);
        calculate.m_TerrainBounds=(Colossal.Mathematics.Bounds3)RawEdgeCapture.Decode(typeof(Colossal.Mathematics.Bounds3),edge.GetProperty("fields").GetProperty("m_TerrainBounds"),"terrainBounds");
        for(int i=0;i<edgeIds.Length;i++)calculate.Execute(i);
        Report("CalculateEdgeGeometry",new[]{typeof(EdgeGeometry),typeof(StartNodeGeometry),typeof(EndNodeGeometry)});
        var heightMap=new ReplayMap<int2,float4>();
        foreach(var c in flatten)foreach(var entity in Roots(c)) {
            var job=WorldStageTests.Bind<FlattenNodeGeometryJob>(world);job.m_EdgeHeightMap=heightMap.Writer;job.Execute(new ReplayChunk(world,new[]{entity}));
        }
        Report("FlattenNodeGeometry",new[]{typeof(NodeGeometry)},heightMap);
        var finishIds=Roots(finish);var finishing=WorldStageTests.Bind<FinishEdgeGeometryJob>(world);
        finishing.m_Entities=new(i=>finishIds[i],(_,_)=>throw new InvalidOperationException("Read-only identity"),finishIds.Length);
        finishing.m_EdgeHeightMap=heightMap;finishing.m_TerrainHeightData=TerrainCapture.Load(finish.GetProperty("fields").GetProperty("m_TerrainHeightData"));
        ReplayTerrain.SampleCount=0;
        for(int i=0;i<finishIds.Length;i++)finishing.Execute(i);
        Report("FinishEdgeGeometry",new[]{typeof(EdgeGeometry)});
        bool passed=reports.All(r=>JsonSerializer.SerializeToElement(r).GetProperty("withinResearchTolerance").GetBoolean());
        var computedEdges=finishIds.Select(e=>new{id=new[]{e.Index,e.Version},
            original=world.Try<Game.Tools.Temp>(e,out var temp)?new[]{temp.m_Original.Index,temp.m_Original.Version}:new[]{e.Index,e.Version},
            edgeGeometry=RawEdgeCapture.Encode(world.Get<EdgeGeometry>(e))}).ToArray();
        File.WriteAllText(output,JsonSerializer.Serialize(new{scope="Computed initialize -> edge -> flatten -> finish; no recorded intermediate overwrites; explicit native query membership",
            passed,toleranceMetres=0.03,toleranceBasis="Dan accepts a few cm of bounded accumulated geometric error; 3 cm spatial scalar/control-point bounds; nonspatial differences and identity/key mismatches remain failures",
            gameSha256=RawEdgeCapture.GameHash,tracePath,stageReports=reports,computedEdges,terrainSamples=ReplayTerrain.SampleCount,reads=world.Reads,writes=world.Writes,
            captures=paths.Select(p=>new{path=p,sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)))})},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Computed 4-stage pipeline: {edgeIds.Length} edges; within research tolerance: {passed}");
        return passed?0:1;
    }
}
