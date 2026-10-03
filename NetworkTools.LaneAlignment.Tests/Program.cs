using System;
using NetworkTools.Geometry;
using Lane = NetworkTools.Geometry.ConnectLaneAlignment.Lane;
using Failure = NetworkTools.Geometry.ConnectLaneAlignment.Failure;

static class Program {
    static int assertions;
    static void Check(bool value, string name) { assertions++; if (!value) throw new Exception(name); }
    static void Near(double actual, double expected, string name) => Check(Math.Abs(actual - expected) < 1e-12, name);
    static Lane[] Group(params double[] offsets) {
        var lanes = new Lane[offsets.Length];
        for (int i = 0; i < lanes.Length; i++) lanes[i] = new Lane(100 + i, i, 1, offsets[i], 3.5);
        return lanes;
    }
    static Lane[] Reverse(Lane[] lanes) {
        var reversed = new Lane[lanes.Length];
        for (int i = 0; i < lanes.Length; i++) {
            var lane = lanes[lanes.Length - 1 - i];
            reversed[i] = new Lane(lane.Id, -lane.PortOrder, -lane.Direction, -lane.Offset, lane.Width);
        }
        return reversed;
    }
    static ConnectLaneAlignment.Result Pass(Lane[] a, Lane[] b, double tolerance = 0) {
        Check(ConnectLaneAlignment.Fit(a,b,tolerance,out var result,out var failure), "expected success: " + failure);
        Check(failure == Failure.None, "success status"); return result;
    }
    static void Fail(Lane[] a, Lane[] b, Failure expected, double tolerance = 0) {
        Check(!ConnectLaneAlignment.Fit(a,b,tolerance,out _,out var failure), "expected failure");
        Check(failure == expected, "failure expected " + expected + " got " + failure);
    }
    static void Main() {
        var two = Group(-1.75,1.75);
        var left = Group(-3.5,0);
        var right = Group(0,3.5);
        Near(Pass(two,left).Shift,1.75,"2->3 left selected pair");
        Near(Pass(two,right).Shift,-1.75,"2->3 right selected pair");
        Near(Pass(left,two).Shift,-1.75,"3->2 left selected pair");
        Near(Pass(right,two).Shift,1.75,"3->2 right selected pair");
        Near(Pass(Reverse(two),Reverse(left)).Shift,-1.75,"reversed frame negates shift");
        Near(Pass(Reverse(two),Reverse(right)).Shift,1.75,"opposite directed carriageway");
        Fail(two,Reverse(left),Failure.DirectionMismatch);
        var mixed = Group(-1.75,1.75); mixed[1].Direction = -1;
        Fail(two,mixed,Failure.MixedDirection);
        var exact = Pass(two,two); Near(exact.Shift,0,"zero parity"); Near(exact.MaximumPositionResidual,0,"exact residual");
        var asym = Pass(Group(-7,-3.2,1),Group(-5,-1.2,3)); Near(asym.Shift,-2,"asymmetric layout");
        var spacing = Group(-1.6,1.6);
        Fail(two,spacing,Failure.IncompatibleSpacing);
        ConnectLaneAlignment.Fit(two,spacing,0,out var rejected,out _);
        Near(rejected.MaximumPositionResidual,.15,"spacing diagnostic");
        var edge = Pass(Group(0,1),Group(.1,1)); Near(edge.MaximumPositionResidual,.05,"5cm boundary");
        Fail(Group(0,1),Group(.100001,1),Failure.IncompatibleSpacing);
        var widths = Group(-1.75,1.75); widths[0].Width = 3.2;
        Fail(two,widths,Failure.IncompatibleWidth);
        var allowed = Pass(two,widths,.31); Near(allowed.Shift,0,"explicit differing width policy");
        Near(allowed.MaximumWidthDifference,.3,"width diagnostic");
        Fail(two,spacing,Failure.IncompatibleSpacing,100);
        var duplicate = Group(-1.75,1.75); duplicate[1].Id = duplicate[0].Id;
        Fail(two,duplicate,Failure.DuplicateIdentity);
        var gap = Group(-1.75,1.75); gap[1].PortOrder = 2;
        Fail(two,gap,Failure.NoncontiguousGroup);
        var order = Group(1.75,-1.75); Fail(two,order,Failure.NoncontiguousGroup);
        var overflowRank = Group(-1.75,1.75); overflowRank[0].PortOrder = int.MaxValue; overflowRank[1].PortOrder = int.MinValue;
        Fail(two,overflowRank,Failure.NoncontiguousGroup);
        Fail(two,Group(0),Failure.UnequalCount);
        Fail(null,two,Failure.InvalidInput); Fail(two,Array.Empty<Lane>(),Failure.InvalidInput);
        Fail(two,two,Failure.InvalidInput,-1); Fail(two,two,Failure.InvalidInput,double.PositiveInfinity);
        foreach (double value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity }) {
            var invalid = Group(-1.75,1.75); invalid[0].Offset = value; Fail(two,invalid,Failure.InvalidInput);
            invalid = Group(-1.75,1.75); invalid[0].Width = value; Fail(two,invalid,Failure.InvalidInput);
        }
        var zeroWidth = Group(-1.75,1.75); zeroWidth[0].Width = 0; Fail(two,zeroWidth,Failure.InvalidInput);
        var direction = Group(-1.75,1.75); direction[0].Direction = 0; Fail(two,direction,Failure.InvalidInput);
        Fail(Group(double.MaxValue),Group(-double.MaxValue),Failure.NonfiniteOutput);
        var huge = Pass(Group(double.MaxValue),Group(0)); Near(huge.Shift / double.MaxValue,1,"finite extreme shift");
        var many = new double[65]; for(int i=0;i<many.Length;i++) many[i]=i;
        Fail(Group(many),Group(many),Failure.CapacityExceeded);
        var max = new double[64]; for(int i=0;i<max.Length;i++) max[i]=i;
        Check(Pass(Group(max),Group(max)).Count == 64,"capacity boundary");
        for (int i=0;i<3;i++) {
            var result = Pass(two,left); Near(result.Shift,1.75,"deterministic repeated fit");
            Near(two[0].Offset,-1.75,"reference unmodified"); Near(left[0].Offset,-3.5,"candidate unmodified");
        }
        // Independent objective checks: no sampled alternative translation can improve
        // the maximum lane error; reversal preserves error and negates the solution.
        for (int trial=0;trial<20;trial++) {
            double d0 = (trial-10)*.001, d1 = .02, d2 = -.01;
            var a = Group(-4+d0,d1,4+d2); var b = Group(-4,0,4);
            var fitted = Pass(a,b);
            double measured = 0;
            for (int j=0;j<3;j++) measured = Math.Max(measured,Math.Abs(fitted.Shift+b[j].Offset-a[j].Offset));
            Near(measured,fitted.MaximumPositionResidual,"reported objective matches physical errors");
            for (int step=-10;step<=10;step++) {
                double alternative = fitted.Shift+step*.001, alternativeError=0;
                for(int j=0;j<3;j++) alternativeError=Math.Max(alternativeError,Math.Abs(alternative+b[j].Offset-a[j].Offset));
                Check(alternativeError+1e-14>=measured,"minimax beats alternative translation");
            }
            var reversed = Pass(Reverse(a),Reverse(b));
            Near(reversed.Shift,-fitted.Shift,"reversed fit physical equivalence");
            Near(reversed.MaximumPositionResidual,fitted.MaximumPositionResidual,"reversal preserves objective");
        }
        Console.WriteLine("ConnectLaneAlignment: " + assertions + " assertions passed (pure offline; no native lane claim).");
    }
}


