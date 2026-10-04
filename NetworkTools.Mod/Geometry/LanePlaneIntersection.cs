namespace NetworkTools.Geometry {
    using System;

    /// <summary>
    /// Horizontal cross-section plane intersection. Proposal projection is not native
    /// geometry evidence. Cubic intersection is analytic derivative-partition isolation,
    /// not sampled-sign detection: all derivative roots in [0,1] split monotone pieces.
    /// Tangencies, near-plane extrema, multiple roots and conditioning uncertainty reject.
    /// Caller owns the physical plane/station, correspondence, and tolerances.
    /// </summary>
    public static class LanePlaneIntersection {
        public readonly struct Point {
            public readonly double X, Y, Z;
            public Point(double x,double y,double z) { X=x; Y=y; Z=z; }
        }
        public readonly struct Cubic {
            public readonly Point A,B,C,D;
            public Cubic(Point a,Point b,Point c,Point d) { A=a; B=b; C=c; D=d; }
        }
        public readonly struct Hit {
            public readonly Point Position;
            // Cubic parameter for IntersectCubic; signed horizontal metres for ProjectTangent.
            public readonly double Parameter;
            public Hit(Point position,double parameter) { Position=position; Parameter=parameter; }
        }
        public enum Failure { None, InvalidInput, NonfiniteCalculation, CoplanarOrNearPlane,
            TangentOrIllConditioned, MultipleIntersections, NoIntersection, ExtrapolationExceeded, ResidualExceeded }
        private static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
        private static bool Finite(Point p) => Finite(p.X) && Finite(p.Y) && Finite(p.Z);
        private static Point Lerp(Point a,Point b,double t) => new Point(a.X*(1-t)+b.X*t,a.Y*(1-t)+b.Y*t,a.Z*(1-t)+b.Z*t);
        private static double Hypot(double x,double z) {
            double scale=Math.Max(Math.Abs(x),Math.Abs(z));
            if(scale==0) return 0;
            return scale*Math.Sqrt((x/scale)*(x/scale)+(z/scale)*(z/scale));
        }
        private static bool Frame(Point origin,double normalX,double normalZ,double minCosine,out double nx,out double nz) {
            nx=nz=0;
            if(!Finite(origin)||!Finite(normalX)||!Finite(normalZ)||!Finite(minCosine)||minCosine<=0||minCosine>1) return false;
            double length=Hypot(normalX,normalZ);
            if(!Finite(length)||length==0) return false;
            nx=normalX/length; nz=normalZ/length; return true;
        }
        private static double Distance(Point p,Point origin,double nx,double nz) => (p.X-origin.X)*nx+(p.Z-origin.Z)*nz;
        private static double Eval(double a,double b,double c,double d,double t) => ((a*t+b)*t+c)*t+d;
        private static Point Position(Cubic curve,double t) => Lerp(Lerp(Lerp(curve.A,curve.B,t),Lerp(curve.B,curve.C,t),t),Lerp(Lerp(curve.B,curve.C,t),Lerp(curve.C,curve.D,t),t),t);

        public static bool ProjectTangent(Point endpoint,Point tangent,Point planeOrigin,double normalX,double normalZ,
            double minimumCrossingCosine,double maximumExtrapolation,out Hit hit,out Failure failure) {
            hit=default; failure=Failure.InvalidInput;
            if(!Frame(planeOrigin,normalX,normalZ,minimumCrossingCosine,out var nx,out var nz)
                || !Finite(endpoint)||!Finite(tangent)||!Finite(maximumExtrapolation)||maximumExtrapolation<0) return false;
            double length=Hypot(tangent.X,tangent.Z);
            if(!Finite(length)||length==0) { failure=Failure.TangentOrIllConditioned; return false; }
            double ux=tangent.X/length,uy=tangent.Y/length,uz=tangent.Z/length;
            double cosine=ux*nx+uz*nz;
            if(Math.Abs(cosine)<minimumCrossingCosine) { failure=Failure.TangentOrIllConditioned; return false; }
            double distance=Distance(endpoint,planeOrigin,nx,nz);
            double along=-distance/cosine;
            if(!Finite(distance)||!Finite(along)) { failure=Failure.NonfiniteCalculation; return false; }
            if(Math.Abs(along)>maximumExtrapolation) { failure=Failure.ExtrapolationExceeded; return false; }
            var point=new Point(endpoint.X+along*ux,endpoint.Y+along*uy,endpoint.Z+along*uz);
            if(!Finite(point)) { failure=Failure.NonfiniteCalculation; return false; }
            hit=new Hit(point,along); failure=Failure.None; return true;
        }

        public static bool IntersectCubic(Cubic curve,Point planeOrigin,double normalX,double normalZ,
            double positionTolerance,double minimumCrossingCosine,out Hit hit,out Failure failure) {
            hit=default; failure=Failure.InvalidInput;
            if(!Frame(planeOrigin,normalX,normalZ,minimumCrossingCosine,out var nx,out var nz)
                ||!Finite(positionTolerance)||positionTolerance<=0||!Finite(curve.A)||!Finite(curve.B)||!Finite(curve.C)||!Finite(curve.D)) return false;
            double d0=Distance(curve.A,planeOrigin,nx,nz),d1=Distance(curve.B,planeOrigin,nx,nz),
                d2=Distance(curve.C,planeOrigin,nx,nz),d3=Distance(curve.D,planeOrigin,nx,nz);
            double scale=Math.Max(Math.Max(Math.Abs(d0),Math.Abs(d1)),Math.Max(Math.Abs(d2),Math.Abs(d3)));
            if(!Finite(scale)) { failure=Failure.NonfiniteCalculation; return false; }
            if(scale<=positionTolerance) { failure=Failure.CoplanarOrNearPlane; return false; }
            double e=positionTolerance/scale;
            // Reject scales where roundoff could dominate the caller's residual policy.
            if(e<1e-12) { failure=Failure.TangentOrIllConditioned; return false; }
            d0/=scale; d1/=scale; d2/=scale; d3/=scale;
            double a=-d0+3*d1-3*d2+d3,b=3*d0-6*d1+3*d2,c=-3*d0+3*d1,d=d0;
            double qa=3*a,qb=2*b,qc=c;
            var stations=new double[4]; int count=2; stations[0]=0; stations[1]=1;
            void Add(double t) { if(t>0&&t<1) { for(int i=0;i<count;i++) if(stations[i]==t) return; stations[count++]=t; } }
            if(qa==0) { if(qb!=0) Add(-qc/qb); }
            else {
                double discriminant=qb*qb-4*qa*qc;
                double uncertainty=1e-14*(qb*qb+Math.Abs(4*qa*qc));
                // Near a repeated derivative root, reject instead of choosing its sign.
                if(Math.Abs(discriminant)<=uncertainty) { failure=Failure.TangentOrIllConditioned; return false; }
                if(discriminant>0) {
                    double sqrt=Math.Sqrt(discriminant);
                    double q=-.5*(qb+(qb>=0?sqrt:-sqrt));
                    Add(q/qa); if(q!=0) Add(qc/q);
                }
            }
            Array.Sort(stations,0,count);
            for(int i=1;i<count-1;i++) if(Math.Abs(Eval(a,b,c,d,stations[i]))<=e) {
                failure=Failure.TangentOrIllConditioned; return false;
            }
            int roots=0; double root=0;
            // Exact endpoint roots are supported; near-end uncertainty rejects.
            for(int i=0;i<count;i+=count-1) {
                double f=i==0?d0:d3;
                if(f==0) { roots++; root=stations[i]; }
                else if(Math.Abs(f)<=e) { failure=Failure.TangentOrIllConditioned; return false; }
            }
            for(int i=0;i<count-1;i++) {
                double lo=stations[i],hi=stations[i+1];
                double flo=i==0?d0:Eval(a,b,c,d,lo), fhi=i+1==count-1?d3:Eval(a,b,c,d,hi);
                if(flo==0||fhi==0||Math.Sign(flo)==Math.Sign(fhi)) continue;
                roots++;
                for(int iteration=0;iteration<80;iteration++) {
                    double mid=(lo+hi)*.5;
                    if(mid==lo||mid==hi) break;
                    double fmid=Eval(a,b,c,d,mid);
                    if(fmid==0) { lo=hi=mid; break; }
                    if(Math.Sign(fmid)==Math.Sign(flo)) { lo=mid; flo=fmid; } else hi=mid;
                }
                root=(lo+hi)*.5;
            }
            if(roots!=1) { failure=roots==0?Failure.NoIntersection:Failure.MultipleIntersections; return false; }
            var point=Position(curve,root);
            var ab=new Point(curve.B.X-curve.A.X,curve.B.Y-curve.A.Y,curve.B.Z-curve.A.Z);
            var bc=new Point(curve.C.X-curve.B.X,curve.C.Y-curve.B.Y,curve.C.Z-curve.B.Z);
            var cd=new Point(curve.D.X-curve.C.X,curve.D.Y-curve.C.Y,curve.D.Z-curve.C.Z);
            var tangent=Lerp(Lerp(ab,bc,root),Lerp(bc,cd,root),root);
            double speed=Hypot(tangent.X,tangent.Z);
            if(!Finite(point)||!Finite(tangent)||!Finite(speed)) { failure=Failure.NonfiniteCalculation; return false; }
            if(speed==0||Math.Abs(tangent.X*nx+tangent.Z*nz)/speed<minimumCrossingCosine) {
                failure=Failure.TangentOrIllConditioned; return false;
            }
            if(Math.Abs(Distance(point,planeOrigin,nx,nz))>positionTolerance) { failure=Failure.ResidualExceeded; return false; }
            hit=new Hit(point,root); failure=Failure.None; return true;
        }
    }
}
