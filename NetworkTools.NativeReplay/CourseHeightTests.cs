using System.Security.Cryptography;
using System.Text.Json;
using Colossal.Mathematics;
using Game.Tools;
using Game.Prefabs;
using Unity.Mathematics;
using NativeReplay;

internal static class CourseHeightTests {
    public static int Run(string path) {
        if(File.Exists(path)) throw new IOException("Refusing to overwrite evidence");
        var hash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(NetCourse).Assembly.Location)));
        if(hash!="AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A")
            throw new InvalidOperationException("Unqualified Game.dll");
        var results=new List<object>();
        var failed=false;
        void Check(string name,bool pass) { results.Add(new {name,pass}); failed|=!pass; }
        var authored=new Bezier4x3 {a=new float3(0,20,0),b=new float3(30,20,0),c=new float3(60,50,0),d=new float3(90,50,0)};
        NetCourse Course()=>new NetCourse {m_Curve=authored,m_StartPosition=new CoursePos{m_Position=authored.a,m_CourseDelta=0},m_EndPosition=new CoursePos{m_Position=authored.d,m_CourseDelta=1}};
        var stage=new ReplayCourseHeight {m_SampleRange=new float2(0,1),m_SampleFactor=3,
            m_Buffer=new[]{new CourseHeightItem{m_CourseHeight=20},new CourseHeightItem{m_CourseHeight=30},new CourseHeightItem{m_CourseHeight=40},new CourseHeightItem{m_CourseHeight=50}}};
        var course=Course();
        stage.SampleCourseHeight(ref course,default(NetGeometryData));
        Check("fixed outer heights retained",course.m_Curve.a.y==20&&course.m_Curve.d.y==50);
        Check("native samples replace authored endpoint grades",math.abs(course.m_Curve.b.y-30)<.001f&&math.abs(course.m_Curve.c.y-40)<.001f);
        Check("horizontal polygon retained",course.m_Curve.a.xz.Equals(authored.a.xz)&&course.m_Curve.b.xz.Equals(authored.b.xz)&&course.m_Curve.c.xz.Equals(authored.c.xz)&&course.m_Curve.d.xz.Equals(authored.d.xz));
        stage.m_Buffer=Enumerable.Range(0,301).Select(i=>new CourseHeightItem{m_CourseHeight=MathUtils.Position(authored,i/300f).y}).ToArray();
        stage.m_SampleFactor=300;
        course=Course();stage.SampleCourseHeight(ref course,default(NetGeometryData));
        Check("authored height samples reconstruct authored controls",math.abs(course.m_Curve.b.y-authored.b.y)<.001f&&math.abs(course.m_Curve.c.y-authored.c.y)<.001f);
        var part=Course();part.m_StartPosition.m_CourseDelta=.2f;part.m_EndPosition.m_CourseDelta=.7f;
        part.m_StartPosition.m_Position=MathUtils.Position(authored,.2f);part.m_EndPosition.m_Position=MathUtils.Position(authored,.7f);
        stage.SampleCourseHeight(ref part,default(NetGeometryData));
        var cut=MathUtils.Cut(authored,new float2(.2f,.7f));
        Check("subinterval grades preserved with authored samples",math.abs(part.m_Curve.b.y-cut.b.y)<.001f&&math.abs(part.m_Curve.c.y-cut.c.y)<.001f);
        Check("native output resets interval",part.m_StartPosition.m_CourseDelta==0&&part.m_EndPosition.m_CourseDelta==1);
        var elevationStage=new ReplayCourseElevation { m_TerrainHeightData=new ReplayTerrainData {
            captured=true,heights=new ushort[256*256],resolution=new int3(256,1,256),scale=new float3(1),offset=new float3(100,0,100)} };
        foreach(var height in new[]{-8f,-4f,-3.99f,0f,3.99f,4f,8f})
        foreach(var minimum in new[]{-100f,0f})
        foreach(var transition in new[]{false,true}) {
            var e=Course();e.m_Curve.a.y=height;e.m_Curve.b.y=height;e.m_Curve.c.y=height;e.m_Curve.d.y=height;
            e.m_StartPosition.m_Flags=transition?CoursePosFlags.LeftTransition:0;
            var upgraded=default(Game.Net.Upgraded);
            elevationStage.CalculateElevation(default,default,ref e,ref upgraded,new NetGeometryData{m_DefaultWidth=16,m_ElevationLimit=4},new PlaceableNetData{m_ElevationRange=new Bounds1(minimum,100)});
            var expected=NetworkTools.Geometry.ConnectCourseElevation.Classify(height,4,minimum,100,transition);
            Check($"native elevation parity {height}/{minimum}/{transition}",e.m_StartPosition.m_Elevation.x==expected);
        }
        File.WriteAllText(path,JsonSerializer.Serialize(new {schemaVersion=1,gameSha256=hash,status=failed?"failed":"passed",
            boundary="Hash-pinned native SampleCourseHeight/SampleHeight with supplied sample buffers; no terrain constructor, ECS splitting, classification, preview or Apply",results},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Course-height native method replay: {results.Count} checks, {(failed?"FAILED":"passed")}");
        return failed?1:0;
    }
}
