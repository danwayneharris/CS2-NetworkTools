using System;
using NetworkTools.Geometry;
using P = NetworkTools.Geometry.PlanarFairing.Point;

internal static class BoundaryRotationTests {
    internal static unsafe void Run() {
        var nodes = new[] { new P(0,0), new P(50,10), new P(100,0) };
        var curves = new[] { new PlanarCubic(nodes[0],new P(15,0),new P(35,10),nodes[1]),
            new PlanarCubic(nodes[1],new P(65,10),new P(85,0),nodes[2]) };
        var output = new PlanarCubic[2]; var fitted = new P[3]; var stations = new double[3];
        fixed(P* n=nodes, f=fitted)
        fixed(PlanarCubic* c=curves, o=output)
        fixed(double* s=stations) {
            double angle = 5*Math.PI/180;
            if(!PlanarPathTarget.Fit(n,c,3,1,f,o,s,out _,out _,0,angle)) throw new Exception("rotation fit rejected");
            var tangent = new P(output[1].D.X-output[1].C.X,output[1].D.Z-output[1].C.Z);
            if(Math.Abs(Math.Atan2(tangent.Z,tangent.X)-angle)>1e-10) throw new Exception("boundary rotation wrong");
            if(fitted[0].X!=0 || fitted[2].X!=100 || fitted[2].Z!=0) throw new Exception("boundary moved");
            var a = new P(output[0].D.X-output[0].C.X,output[0].D.Z-output[0].C.Z);
            var b = new P(output[1].B.X-output[1].A.X,output[1].B.Z-output[1].A.Z);
            if(Math.Abs(a.X*b.Z-a.Z*b.X)>1e-8) throw new Exception("target tangent discontinuity");
            if(!PlanarPathTarget.Fit(n,c,3,0,f,o,s,out _,out _,angle,angle)) throw new Exception("zero rejected");
            if(output[0].B.X!=curves[0].B.X || output[1].C.Z!=curves[1].C.Z) throw new Exception("zero changed input");
            if(PlanarPathTarget.Fit(n,c,3,1,f,o,s,out _,out _,double.NaN,0)) throw new Exception("NaN accepted");
            if(PlanarPathTarget.Fit(n,c,3,1,f,o,s,out _,out _,1,0)) throw new Exception("unbounded rotation accepted");
        }
        Console.WriteLine("Boundary rotation checks passed");
    }
}
