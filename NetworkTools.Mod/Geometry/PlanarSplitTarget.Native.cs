namespace NetworkTools.Geometry {
    using System;
    using Point = PlanarFairing.Point;

    public static partial class PlanarSplitTarget {
        /// <summary>Caller-owned scratch; outputs are candidates until true is returned.</summary>
        public static unsafe bool Fit(Point* nodes, PlanarCubic* curves, byte* splits, int count,
            double strength, Point* outputNodes, PlanarCubic* outputCurves,
            Point* workNodes, PlanarCubic* workCurves, double* stations,
            out SmoothFailure failure, out int index, double startRotation = 0, double endRotation = 0) {
            failure=SmoothFailure.InvalidArguments; index=-1;
            if(nodes==null || curves==null || splits==null || outputNodes==null || outputCurves==null
                || workNodes==null || workCurves==null || stations==null || count<2 || count>512
                || double.IsNaN(strength) || strength<0 || strength>1 || splits[0]!=0 || splits[count-1]!=0) return false;
            var anySplit=false;
            for(var i=0;i<count;i++) {
                if(!Finite(nodes[i].X)||!Finite(nodes[i].Z))
                    return PlanarPathTarget.Fail(SmoothFailure.NonFiniteNode,i,out failure,out index);
                anySplit|=splits[i]!=0;
            }
            if(!anySplit) return PlanarPathTarget.Fit(nodes,curves,count,strength,outputNodes,outputCurves,
                stations,out failure,out index,startRotation,endRotation);
            for(var i=0;i<count-1;i++) {
                var c=curves[i];
                if(!Finite(c.A.X)||!Finite(c.A.Z)||!Finite(c.B.X)||!Finite(c.B.Z)
                    ||!Finite(c.C.X)||!Finite(c.C.Z)||!Finite(c.D.X)||!Finite(c.D.Z))
                    return PlanarPathTarget.Fail(SmoothFailure.NonFiniteCurve,i,out failure,out index);
            }
            for(var i=1;i<count-1;i++)
                if(nodes[i].Fixed) return PlanarPathTarget.Fail(SmoothFailure.InteriorPinnedNode,i,out failure,out index);
            for(var start=0;start<count-1;) {
                var end=start+1; while(end<count-1 && splits[end]==0) end++;
                var n=end-start+1;
                for(var i=0;i<n;i++) workNodes[i]=nodes[start+i];
                for(var i=0;i<n-1;i++) workCurves[i]=curves[start+i];
                if(start>0) {
                    if(!SplitDirection(nodes,splits,count,start,out var direction))
                        return PlanarPathTarget.Fail(SmoothFailure.BoundaryTangents,start,out failure,out index);
                    var c=workCurves[0]; c.A=nodes[start]; c.B=Add(c.A,direction,1); workCurves[0]=c;
                }
                if(end<count-1) {
                    if(!SplitDirection(nodes,splits,count,end,out var direction))
                        return PlanarPathTarget.Fail(SmoothFailure.BoundaryTangents,end,out failure,out index);
                    var c=workCurves[n-2]; c.D=nodes[end]; c.C=Add(c.D,direction,-1); workCurves[n-2]=c;
                }
                if(!PlanarPathTarget.Fit(workNodes,workCurves,n,1,outputNodes+start,outputCurves+start,
                    stations,out failure,out index,start==0?startRotation:0,end==count-1?endRotation:0)) {
                    if(index>=0) index+=start; return false;
                }
                start=end;
            }
            for(var i=0;i<count;i++) outputNodes[i]=Mix(nodes[i],outputNodes[i],strength);
            for(var i=0;i<count-1;i++) {
                var old=curves[i]; var target=outputCurves[i];
                outputCurves[i]=new PlanarCubic(Mix(old.A,target.A,strength),Mix(old.B,target.B,strength),
                    Mix(old.C,target.C,strength),Mix(old.D,target.D,strength));
            }
            // Enabling a split imposes a hard planar join constraint independently of
            // strength, including zero. Strength controls the rest of each section.
            for(var i=1;i<count-1;i++) if(splits[i]!=0) {
                if(!SplitDirection(nodes,splits,count,i,out var direction)) return false;
                outputNodes[i]=nodes[i]; var left=outputCurves[i-1]; var right=outputCurves[i];
                var l=Distance(left.D,left.C); var r=Distance(right.A,right.B);
                if(!Finite(l) || !Finite(r) || l<0.01 || r<0.01)
                    return PlanarPathTarget.Fail(SmoothFailure.BoundaryTangents,i,out failure,out index);
                left.D=nodes[i]; left.C=Add(nodes[i],direction,-l);
                right.A=nodes[i]; right.B=Add(nodes[i],direction,r);
                outputCurves[i-1]=left; outputCurves[i]=right;
            }
            failure=SmoothFailure.None; index=-1; return true;
        }

        private static unsafe bool SplitDirection(Point* nodes,byte* splits,int count,int at,out Point direction) {
            var before=at-1; while(before>0 && splits[before]==0) before--;
            var after=at+1; while(after<count-1 && splits[after]==0) after++;
            return PlanarBezier.Tangent(nodes[before],nodes[at],nodes[after],out direction);
        }
        private static Point Add(Point a,Point b,double scale)=>new Point(a.X+scale*b.X,a.Z+scale*b.Z);
        private static Point Mix(Point a,Point b,double t)=>new Point(a.X+t*(b.X-a.X),a.Z+t*(b.Z-a.Z));
        private static double Distance(Point a,Point b)=>Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z));
        private static bool Finite(double n)=>!double.IsNaN(n)&&!double.IsInfinity(n);
    }
}
