using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Colossal.Mathematics;
using NetworkTools.Systems.Tools.RoadShape;
using Unity.Collections;
using Unity.Mathematics;
static class CapturedSlopeReplay {
 static float3 V(JsonElement a)=>new(a[0].GetSingle(),a[1].GetSingle(),a[2].GetSingle());
 static float G(Bezier4x3 b,float t){var d=3*((1-t)*(1-t)*(b.b-b.a)+2*(1-t)*t*(b.c-b.b)+t*t*(b.d-b.c));return 100*d.y/math.length(d.xz);}
 public static void Run(string tracePath,string permanentPath,string output){
  var text=File.ReadAllText(tracePath);using var doc=JsonDocument.Parse(text.Substring(text.IndexOf('{')));var tr=doc.RootElement;
  var ns=tr.GetProperty("nodes");var nodes=ns.EnumerateArray().Select(n=>V(n.GetProperty("output"))).ToArray();
  if(ns[0].GetProperty("incidentEdges").GetArrayLength()!=3 || ns[nodes.Length-1].GetProperty("incidentEdges").GetArrayLength()!=1)throw new Exception("Endpoint smoothing eligibility changed");
  var original=tr.GetProperty("edges").EnumerateArray().Select(e=>{
   if(!e.GetProperty("forward").GetBoolean())throw new Exception("Fixture requires forward traversal");
   var p=e.GetProperty("output");var b=new Bezier4x3(V(p[0]),V(p[1]),V(p[2]),V(p[3]));
   return new EdgeState{IsForward=true,Bezier=b,Length=MathUtils.Length(b),OriginalBezierA=b.a,OriginalBezierD=b.d};}).ToArray();
  var ids=tr.GetProperty("edges").EnumerateArray().Select(e=>e.GetProperty("entity").GetString()).ToArray();
  using var pd=JsonDocument.Parse(File.ReadAllText(permanentPath));
  var actual=pd.RootElement.GetProperty("result").GetProperty("edges").EnumerateArray().ToDictionary(e=>e.GetProperty("index").GetInt32()+":"+e.GetProperty("version").GetInt32());
  var reports=new List<object>();
  foreach(var variant in new[]{"merged-main-4b5fc1c","handle-fix-only","current-with-alignment"}){
   var edges=(EdgeState[])original.Clone();float distance=0;
   for(int i=0;i<edges.Length;i++){
    ref var e=ref edges[i];e.CalculateControlPointRatios();
    // Historical formula copied from merged main; other transform operations use production code.
    if(variant=="merged-main-4b5fc1c")e.EndControlPointRatio=math.clamp(math.distance(e.Bezier.a.xz,e.Bezier.c.xz)/e.Length,0,1);
    if(i>0)distance+=math.distance(edges[i-1].Bezier.d,nodes[i]);
    distance+=math.distance(nodes[i],e.Bezier.a);e.CumulativeDistance=distance;distance+=e.Length;
   }
   distance+=math.distance(edges[edges.Length-1].Bezier.d,nodes[nodes.Length-1]);
   var ctx=ShapeTransformContext.Create(nodes[0],nodes[nodes.Length-1]);ctx.TotalLength=distance;
   var config=new ShapeJobConfig();var transform=new SlopeLinearTransform();NativeArray<EdgeState> unused=default;
   transform.PreProcess(ref unused,in ctx,in config);
   for(int i=0;i<edges.Length;i++){
    ref var e=ref edges[i];e.StartPointAbsoluteRatio=e.CumulativeDistance/distance;e.EndPointAbsoluteRatio=(e.CumulativeDistance+e.Length)/distance;
    e.StartControlPointAbsoluteRatio=(e.CumulativeDistance+e.StartControlPointRatio*e.Length)/distance;e.EndControlPointAbsoluteRatio=(e.CumulativeDistance+e.EndControlPointRatio*e.Length)/distance;
    transform.Process(ref e,i,in ctx,in config);
   }
   // Managed-array equivalent of TransformPipeline node averaging; no native allocator needed.
   var heights=nodes.Select(n=>n.y).ToArray();
   for(int i=1;i<nodes.Length-1;i++)heights[i]+=(edges[i-1].Bezier.d.y-original[i-1].Bezier.d.y+edges[i].Bezier.a.y-original[i].Bezier.a.y)/2;
   if(variant=="current-with-alignment")for(int i=0;i<edges.Length;i++){
    SlopeUtils.AlignEndpointHeight(ref edges[i],true,nodes[i].y,heights[i]);SlopeUtils.AlignEndpointHeight(ref edges[i],false,nodes[i+1].y,heights[i+1]);
   }
   float error=0;var rows=new List<object>();
   for(int i=0;i<edges.Length;i++){
    var b=edges[i].Bezier;var gs=Enumerable.Range(0,1001).Select(j=>G(b,j/1000f)).ToArray();var points=new[]{b.a,b.b,b.c,b.d};var ps=actual[ids[i]].GetProperty("curve");
    for(int j=0;j<4;j++)error=math.max(error,math.distance(points[j],new float3(ps[j].GetProperty("x").GetSingle(),ps[j].GetProperty("y").GetSingle(),ps[j].GetProperty("z").GetSingle())));
    rows.Add(new{edge=ids[i],startGrade=gs[0],endGrade=gs[1000],minGrade=gs.Min(),maxGrade=gs.Max(),heightGap=i==0?0:b.a.y-edges[i-1].Bezier.d.y,gradeJump=i==0?0:G(b,0)-G(edges[i-1].Bezier,1)});
   }
   reports.Add(new{variant,totalStationLength=distance,maxControlErrorVsCapturedPermanent=error,edges=rows});
   if(variant=="current-with-alignment" && error>.001f)throw new Exception("Replay mismatch: "+error+"m");
  }
  var json=JsonSerializer.Serialize(reports,new JsonSerializerOptions{WriteIndented=true});File.WriteAllText(output,json);Console.WriteLine(json);
 }
}
