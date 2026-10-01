using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using NetworkTools.Geometry;

internal static class SurfaceResponseTests {
    private static unsafe bool Solve(double[] matrix, double[] rhs, double[] solution, double tolerance = .05) {
        fixed (double* a = matrix, b = rhs, x = solution) {
            return SurfaceProfileResponseFit.Fit(a, b, rhs.Length, x, tolerance, 20);
        }
    }
    internal static void Run() {
        var matrix = new double[] { 1,0,0, 0,1,0, 0,0,1, 1,1,1 };
        var rhs = new double[] { 2,-3,4,3 }; var solution = new double[3];
        if (!Solve(matrix,rhs,solution) || Math.Abs(solution[0]-2)>1e-8 || Math.Abs(solution[1]+3)>1e-8 || Math.Abs(solution[2]-4)>1e-8) { throw new Exception("Affine response recovery failed"); }
        if (Solve(new double[12],rhs,solution)) { throw new Exception("Singular fit accepted"); }
        rhs[3] = 100;
        if (Solve(matrix,rhs,solution)) { throw new Exception("Incompatible surface accepted"); }
        rhs[3] = double.NaN;
        if (Solve(matrix,rhs,solution)) { throw new Exception("Nonfinite surface accepted"); }
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"surface-response.json")));
        var data = doc.RootElement;
        matrix = data.GetProperty("responses").EnumerateArray().SelectMany(r => r.EnumerateArray().Select(x => x.GetDouble())).ToArray();
        rhs = data.GetProperty("residual").EnumerateArray().Select(x => x.GetDouble()).ToArray();
        if (!Solve(matrix,rhs,solution)) { throw new Exception("Captured native response rejected"); }
        var expected = data.GetProperty("parameters").EnumerateArray().Select(x => x.GetDouble()).ToArray();
        for (var i=0;i<3;i++) { if (Math.Abs(solution[i]-expected[i])>1e-7) { throw new Exception("Captured response solution mismatch"); } }
        // Equivalent coordinate translation cancels in the response/residual formulation.
        if (Solve(matrix,rhs,solution,.001)) { throw new Exception("Unachievable precision accepted"); }
        Console.WriteLine("Surface response: analytic, singular/nonfinite, incompatible and native captured-case tests passed.");
    }
}
