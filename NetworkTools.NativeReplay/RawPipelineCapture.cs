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

    public static int Run(string tracePath,string output,bool includeJunction=false,bool includePublication=false) {
        if(File.Exists(output))throw new IOException("Refusing to overwrite evidence");
        var native=typeof(GeometrySystem).Assembly;
        if(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(native.Location)))!=RawEdgeCapture.GameHash)throw new ArgumentException("Game hash changed");
        var status=Read(tracePath).GetProperty("result");
        if(status.GetProperty("fault").ValueKind!=JsonValueKind.Null || status.GetProperty("insideCapturedPass").GetBoolean()
            || status.GetProperty("remainingPasses").GetInt32()!=0 || status.GetProperty("gameSha256").GetString()!=RawEdgeCapture.GameHash)
            throw new ArgumentException("Incomplete trace");
        var paths=status.GetProperty("events").EnumerateArray().Where(e=>e.TryGetProperty("path",out _)).Select(e=>e.GetProperty("path").GetString()!).ToArray();
        var captures=paths.Select(Read).Where(c=>(includeJunction||Job(c)!="CalculateNodeGeometryJob")
            && (includePublication||!new[]{"CalculateIntersectionGeometryJob","CopyNodeGeometryJob","UpdateNodeGeometryJob"}.Contains(Job(c)))).ToArray();
        foreach(var c in captures) {
            if(c.GetProperty("schemaVersion").GetInt32()!=1 || c.GetProperty("gameModuleVersionId").GetString()!=native.ManifestModule.ModuleVersionId.ToString()
                || c.GetProperty("operationId").GetString()!=status.GetProperty("operationId").GetString()
                || c.GetProperty("citySession").GetString()!=status.GetProperty("citySession").GetString()
                || c.GetProperty("pass").GetInt32()!=1)throw new ArgumentException("Capture identity/schema mismatch");
            var errors=c.GetProperty("errors").EnumerateArray().Select(e=>e.GetString()).ToArray();
            if(errors.Any(e=>!(Job(c)=="FlattenNodeGeometryJob" && e=="job_field:m_EdgeHeightMap")
                && !(Job(c)=="CalculateIntersectionGeometryJob" && c.GetProperty("phase").GetString()=="entry" && e=="job_field:m_BufferedData"
                    && c.GetProperty("fields").GetProperty("m_BufferedData").GetProperty("error").GetString()=="InvalidOperationException: nonfinite_float")))throw new ArgumentException("Uncaptured stage dependency");
        }
        JsonElement[] Stage(string name,string phase)=>captures.Where(c=>Job(c)==name+"Job" && c.GetProperty("phase").GetString()==phase).ToArray();
        var init=Stage("InitializeNodeGeometry","entry");var edge=Stage("CalculateEdgeGeometry","entry").Single();
        var flatten=Stage("FlattenNodeGeometry","entry");var finish=Stage("FinishEdgeGeometry","entry").Single();
        var junction=Stage("CalculateNodeGeometry","entry");
        if(includeJunction && !junction.Select(c=>c.GetProperty("fields").GetProperty("m_IterationIndex").GetInt32()).SequenceEqual(new[]{0,1}))
            throw new ArgumentException("Expected ordered junction iterations 0 and 1");
        foreach(var c in junction) {
            int iteration=c.GetProperty("fields").GetProperty("m_IterationIndex").GetInt32();
            var after=Stage("CalculateNodeGeometry","exit").Single(x=>x.GetProperty("fields").GetProperty("m_IterationIndex").GetInt32()==iteration);
            if(!Roots(c).SequenceEqual(Roots(after)) || !Roots(c).SequenceEqual(Roots(finish)))throw new ArgumentException("Junction membership changed");
        }
        var initialNodes=init.SelectMany(Roots).ToHashSet();var edgeIds=Roots(edge);var edgeSet=edgeIds.ToHashSet();
        var publication=includePublication?new[]{"CalculateIntersectionGeometry","CopyNodeGeometry","UpdateNodeGeometry"}:Array.Empty<string>();
        foreach(string stage in publication) {
            var before=Stage(stage,"entry").SelectMany(Roots).ToArray();var after=Stage(stage,"exit").SelectMany(Roots).ToArray();
            if(before.Length==0 || !before.SequenceEqual(after) || before.Distinct().Count()!=before.Length)throw new ArgumentException("Publication membership changed: "+stage);
            if(stage=="UpdateNodeGeometry" ? !before.ToHashSet().SetEquals(initialNodes) : !before.SequenceEqual(Roots(finish)))throw new ArgumentException("Publication domain changed: "+stage);
        }
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
        foreach(var c in junction)Import(c,4);
        foreach(string stage in publication)foreach(var c in Stage(stage,"entry"))Import(c,5);
        var world=new ReplayWorld();
        foreach(var (key,cell) in cells) {
            string? presence=cell.GetProperty("presence").GetString();
            if(presence=="absent")typeof(ReplayWorld).GetMethod("Absent")!.MakeGenericMethod(key.Item2).Invoke(world,new object[]{key.Item1});
            else if(presence=="present")typeof(ReplayWorld).GetMethod("Record")!.MakeGenericMethod(key.Item2).Invoke(world,new[]{(object)key.Item1,RawEdgeCapture.Decode(key.Item2,cell.GetProperty("value"),key.ToString())});
            else throw new ArgumentException("Unknown input: "+key);
        }
        var reports=new List<object>();
        void Report(string name,Type[] types,ReplayMap<int2,float4>? map=null,int? iteration=null,ReplayList<IntersectionData>? intersections=null) {
            var diffs=new List<object>();int count=0;
            foreach(var expected in Stage(name,"exit").Where(c=>!iteration.HasValue||c.GetProperty("fields").GetProperty("m_IterationIndex").GetInt32()==iteration.Value)) {
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
            if(intersections!=null) {
                var expected=Stage(name,"exit").Single().GetProperty("fields").GetProperty("m_BufferedData");
                if(expected.GetArrayLength()!=intersections.Length)throw new ArgumentException("Intersection buffer length changed");
                for(int i=0;i<intersections.Length;i++)RawEdgeCapture.Compare(intersections[i],expected[i],"IntersectionData/"+i,diffs,ref count);
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
            var encoded=JsonSerializer.SerializeToElement(diffs);double maximum=0,syncMaximum=0;bool accepted=true;
            var controlErrors=new Dictionary<string,double>();
            foreach(var d in encoded.EnumerateArray()) {
                string path=d.GetProperty("path").GetString()!;
                bool spatial=path.Contains("/EdgeGeometry/")||path.Contains("/StartNodeGeometry/")||path.Contains("/EndNodeGeometry/")||path.Contains("/HeightMap/")||path.Contains("/NodeGeometry/m_Position")||path.Contains("/NodeGeometry/m_Offset")||path.StartsWith("IntersectionData/")||(name=="UpdateNodeGeometry"&&path.Contains("/NodeGeometry/m_Bounds/"));
                if(d.GetProperty("native").ValueKind!=JsonValueKind.Number||!spatial){accepted=false;continue;}
                double nativeValue=d.GetProperty("native").GetDouble(),replayValue=d.GetProperty("replay").GetDouble();
                double delta=Math.Abs(nativeValue-replayValue);
                // Junction m_Middle stores branch markers here, not world positions.
                // Negative segment lengths signal deferred middle connections.
                if((name!="CopyNodeGeometry"&&path.Contains("/m_Geometry/m_Middle/")) || (path.Contains("/m_Length/") && (nativeValue<0||replayValue<0))) {accepted=false;continue;}
                if(path.Contains("/m_SyncVertexTargets")) {
                    syncMaximum=Math.Max(syncMaximum,delta);
                    // Unitless interpolation parameters: do not apply metres to them.
                    if(delta>0.00001 || (nativeValue==0||nativeValue==1||replayValue==0||replayValue==1))accepted=false;
                    continue;
                }
                maximum=Math.Max(maximum,delta);
                if(System.Text.RegularExpressions.Regex.IsMatch(path,@"/(m_Left|m_Right|m_Middle|m_StartMiddle|m_EndMiddle)/[abcd]/[xyz]$")) {
                    string control=path.Substring(0,path.Length-2);
                    controlErrors[control]=controlErrors.GetValueOrDefault(control)+delta*delta;
                }
                if(delta>0.03)accepted=false;
            }
            double controlMaximum=controlErrors.Count==0?0:Math.Sqrt(controlErrors.Values.Max());
            if(controlMaximum>0.03)accepted=false;
            reports.Add(new{stage=name,iteration,comparedScalarFields=count,exact=diffs.Count==0,withinResearchTolerance=accepted,maxSpatialComponentErrorMetres=maximum,maxCurveControlPointErrorMetres=controlMaximum,maxSyncParameterError=syncMaximum,differences=diffs});
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
        foreach(var capture in junction) {
            var job=WorldStageTests.Bind<CalculateNodeGeometryJob>(world);var ids=Roots(capture);
            job.m_Entities=new(i=>ids[i],(_,_)=>throw new InvalidOperationException("Read-only identity"),ids.Length);
            job.m_IterationIndex=capture.GetProperty("fields").GetProperty("m_IterationIndex").GetInt32();
            for(int i=0;i<ids.Length;i++)job.Execute(i);
            Report("CalculateNodeGeometry",new[]{typeof(StartNodeGeometry),typeof(EndNodeGeometry)},iteration:job.m_IterationIndex);
        }
        if(includePublication) {
            var capture=Stage("CalculateIntersectionGeometry","entry").Single();var ids=Roots(capture);
            var scratch=new ReplayList<IntersectionData>(ids.Length,0);
            // AllocateBuffersJob resizes this indexed output buffer before use.
            // Its native recorded contents are never inputs to the prediction.
            var recordedScratch=capture.GetProperty("fields").GetProperty("m_BufferedData");
            if(recordedScratch.ValueKind==JsonValueKind.Array && recordedScratch.GetArrayLength()!=ids.Length)throw new ArgumentException("Scratch allocation size mismatch");
            for(int i=0;i<ids.Length;i++)scratch.Add(default);
            var job=WorldStageTests.Bind<CalculateIntersectionGeometryJob>(world);
            job.m_Entities=new(i=>ids[i],(_,_)=>throw new InvalidOperationException("Read-only identity"),ids.Length);
            job.m_BufferedData=scratch;job.m_TerrainHeightData=TerrainCapture.Load(capture.GetProperty("fields").GetProperty("m_TerrainHeightData"));
            for(int i=0;i<ids.Length;i++)job.Execute(i);
            Report("CalculateIntersectionGeometry",Array.Empty<Type>(),intersections:scratch);
            var copy=WorldStageTests.Bind<CopyNodeGeometryJob>(world);
            copy.m_Entities=job.m_Entities;copy.m_BufferedData=scratch;
            for(int i=0;i<ids.Length;i++)copy.Execute(i);
            Report("CopyNodeGeometry",new[]{typeof(StartNodeGeometry),typeof(EndNodeGeometry)});
            foreach(var c in Stage("UpdateNodeGeometry","entry")) {
                var update=WorldStageTests.Bind<UpdateNodeGeometryJob>(world);
                update.m_TerrainHeightData=TerrainCapture.Load(c.GetProperty("fields").GetProperty("m_TerrainHeightData"));
                update.Execute(new ReplayChunk(world,Roots(c)));
            }
            Report("UpdateNodeGeometry",new[]{typeof(NodeGeometry)});
        }
        bool passed=reports.All(r=>JsonSerializer.SerializeToElement(r).GetProperty("withinResearchTolerance").GetBoolean());
        var computedEdges=finishIds.Select(e=>new{id=new[]{e.Index,e.Version},
            original=world.Try<Game.Tools.Temp>(e,out var temp)?new[]{temp.m_Original.Index,temp.m_Original.Version}:new[]{e.Index,e.Version},
            edgeGeometry=RawEdgeCapture.Encode(world.Get<EdgeGeometry>(e)),
            startNodeGeometry=includeJunction?RawEdgeCapture.Encode(world.Get<StartNodeGeometry>(e)):null,
            endNodeGeometry=includeJunction?RawEdgeCapture.Encode(world.Get<EndNodeGeometry>(e)):null}).ToArray();
        File.WriteAllText(output,JsonSerializer.Serialize(new{scope="Computed initialize -> edge -> flatten -> finish"+(includeJunction?" -> junction iterations 0/1":"")+(includePublication?" -> intersection -> copy -> node bounds":"")+"; no recorded intermediate overwrites; explicit native query membership",
            passed,toleranceMetres=0.03,toleranceBasis="Dan accepts a few cm of bounded accumulated geometric error; 3 cm spatial scalar/control-point bounds; nonspatial differences and identity/key mismatches remain failures",
            gameSha256=RawEdgeCapture.GameHash,tracePath,stageReports=reports,computedEdges,terrainSamples=ReplayTerrain.SampleCount,reads=world.Reads,writes=world.Writes,
            scratchPolicy=includePublication?"AllocateBuffers.ResizeUninitialized(entities.Length) reproduced as fresh output storage; entry bytes never read; native entry nonfinite scratch is excluded, computed exit compared":null,
            captures=paths.Select(p=>new{path=p,sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)))})},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Computed {(includePublication?8:includeJunction?5:4)}-stage pipeline: {edgeIds.Length} edges; within research tolerance: {passed}");
        return passed?0:1;
    }
}
