using NetworkTools.Systems.Tools.Connect;
using P = NetworkTools.Systems.Tools.Connect.LaneDirectionPolicy;

internal static class Program {
    private static int s_Checks;
    private static void Check(bool value, string message) { s_Checks++; if (!value) throw new Exception(message); }
    private static bool Near(double a, double b) => Math.Abs(a - b) < 1e-8;
    private static void Main() {
        var a = new P.Point(0, 0, 0); var b = new P.Point(3, 1.5, 4);
        var c = new P.Point(6, 3, 8); var d = new P.Point(9, 4.5, 12);
        bool Resolve(bool departure, bool atEnd, double deltaA, double deltaD, bool inverted,
            out P.Direction result, out string reason) => P.TryDirection(departure, atEnd, deltaA, deltaD, inverted, a,b,c,d,out result,out reason);
        Check(Resolve(true,true,0,1,false,out var incoming,out _), "incoming lane is departure source");
        Check(Near(incoming.X,.6) && Near(incoming.Z,.8) && Near(incoming.Grade,.3), "incoming XZ norm and Y/horizontal grade");
        Check(Resolve(true,false,1,0,true,out var storedReverse,out _), "reverse stored edge changes delta/invert only");
        Check(Near(storedReverse.X,incoming.X) && Near(storedReverse.Z,incoming.Z) && Near(storedReverse.Grade,incoming.Grade), "storage reversal preserves physical direction and grade");
        Check(Resolve(false,false,0,1,false,out var outgoing,out _), "outgoing lane is arrival target");
        Check(Near(outgoing.X,-.6) && Near(outgoing.Z,-.8) && Near(outgoing.Grade,-.3), "end handle points against travel, including signed grade");
        Check(Resolve(false,true,1,0,true,out var reversedOutgoing,out _), "outgoing stored reversal supported");
        Check(Near(reversedOutgoing.Grade,outgoing.Grade) && Near(reversedOutgoing.X,outgoing.X), "outgoing storage invariance");
        Check(!Resolve(false,true,0,1,false,out _,out var reason) && reason=="lane_wrong_travel_role", "reversed selection cannot reverse a one-way lane");
        Check(!Resolve(true,false,0,1,false,out _,out reason) && reason=="lane_wrong_travel_role", "departing from outgoing lane is ineligible");
        Check(Resolve(true,true,.5,1,false,out _,out _), "end half-segment reaches node");
        Check(Resolve(false,false,0,.5,false,out _,out _), "start half-segment reaches node");
        Check(!Resolve(true,true,0,.5,false,out _,out reason) && reason=="lane_not_at_endpoint", "middle segment cannot stand in for endpoint");
        Check(!Resolve(true,true,1,1,false,out _,out reason) && reason=="lane_endpoint_ambiguous", "both lane ends at station rejected");
        Check(!Resolve(true,true,0,1,true,out _,out reason) && reason=="lane_composition_direction_mismatch", "composition direction mismatch rejected");
        Check(!Resolve(true,true,double.NaN,1,false,out _,out reason) && reason=="lane_source_nonfinite", "nonfinite station rejected");
        Check(!Resolve(true,true,0,1-1e-9,false,out _,out reason) && reason=="lane_not_at_endpoint", "no guessed near-end station");
        Check(!P.TryDirection(true,true,0,1,false,a,b,d,d,out _,out reason) && reason=="lane_tangent_invalid", "zero horizontal tangent rejected");
        Check(!P.TryDirection(true,true,0,1,false,new P.Point(double.NaN,0,0),b,c,d,out _,out reason), "nonendpoint curve NaN rejected");
        Check(!P.TryDirection(true,true,0,1,false,a,b,new P.Point(9,0,12),d,out _,out reason), "vertical-only tangent rejected");
        Check(P.TryDirection(true,true,0,1,false,a,b,new P.Point(6,6,8),d,out var falling,out _) && Near(falling.Grade,-.3), "downhill grade preserved");

        var k1=new P.Key(11,1); var k2=new P.Key(37,4); var k3=new P.Key(3,9);
        P.Choice Choice(P.Key key,double x,int carriage=0,int group=0,double angle=0,double grade=.3,bool incomingRole=true,string rejection=null)
            => new(key,x,carriage,group,new P.Direction(Math.Cos(angle*Math.PI/180),Math.Sin(angle*Math.PI/180),grade,incomingRole),rejection);
        var choices=new[]{Choice(k3,2,grade:.9),Choice(k1,-2,grade:.1),Choice(k2,0,grade:.5)};
        bool Group(P.Key[] selected,out P.Choice result,out string error,double[] positions=null,P.Choice[] options=null)
            => P.TryGroup(options??choices,positions??new double[]{-2,0,2},selected,out result,out error);
        Check(Group(new[]{k2,k1},out var representative,out _), "input order and nonadjacent numeric ids do not affect lateral group");
        Check(representative.Identity.Equals(k1) && Near(representative.Direction.Grade,.1), "even group uses lower central member and its real grade, no averaging");
        Check(Group(new[]{k3,k1,k2},out representative,out _) && representative.Identity.Equals(k2) && Near(representative.Direction.Grade,.5), "odd group takes central lateral member grade");
        Check(Group(new[]{k2},out representative,out _), "single ordinary lane accepted");
        Check(!Group(Array.Empty<P.Key>(),out _,out reason) && reason=="lane_choice_required", "missing choice rejected");
        Check(!Group(new[]{k1,k1},out _,out reason) && reason=="lane_group_duplicate", "duplicate selection rejected");
        Check(!Group(new[]{new P.Key(11,2)},out _,out reason) && reason=="lane_choice_stale", "entity version reuse cannot restore choice");
        Check(!Group(new[]{k1,k3},out _,out reason) && reason=="lane_group_not_contiguous", "unselected intervening physical lane blocks group");
        Check(!Group(new[]{k1,k2},out _,out reason,new double[]{-2,-1,0,2}) && reason=="lane_group_not_contiguous", "missing/disconnected composition road lane blocks contiguity");
        Check(!Group(new[]{k1,k2},out _,out reason,options:new[]{Choice(k1,-2),Choice(k2,0,carriage:1)}) && reason=="lane_group_incompatible", "different carriageways rejected");
        Check(!Group(new[]{k1,k2},out _,out reason,options:new[]{Choice(k1,-2),Choice(k2,0,group:1)}) && reason=="lane_group_incompatible", "different native groups rejected");
        Check(!Group(new[]{k1,k2},out _,out reason,options:new[]{Choice(k1,-2),Choice(k2,0,incomingRole:false)}) && reason=="lane_group_incompatible", "mixed travel roles rejected");
        Check(Group(new[]{k1,k2},out _,out _,options:new[]{Choice(k1,-2),Choice(k2,0,angle:.9)}), "supported common direction within tolerance");
        Check(!Group(new[]{k1,k2},out _,out reason,options:new[]{Choice(k1,-2),Choice(k2,0,angle:1.1)}) && reason=="lane_group_direction_conflict", "materially conflicting tangents rejected rather than averaged");
        Check(!Group(new[]{k1},out _,out reason,options:new[]{Choice(k1,-2,rejection:"lane_type_unsupported")}) && reason=="lane_type_unsupported", "adapter eligibility reason remains authoritative");
        Check(!Group(new[]{k1},out _,out reason,options:new[]{Choice(k1,-2),Choice(k1,2)}) && reason=="lane_membership_ambiguous", "duplicate source identity rejected");
        Check(!Group(new[]{k1},out _,out reason,options:new[]{Choice(k1,0),Choice(k2,0)}) && reason=="lane_lateral_order_ambiguous", "ambiguous lateral positions rejected");
        Check(!Group(new[]{k1},out _,out reason,positions:new[]{double.NaN}) && reason=="lane_source_nonfinite", "nonfinite physical geometry rejected");
        Check(!Group(new[]{k1},out _,out reason,options:new[]{Choice(k1,double.NaN)}) && reason=="lane_source_nonfinite", "nonfinite diagram position rejected");
        Check(!Group(new[]{k1},out _,out reason,options:new[]{Choice(k1,-2,grade:double.NaN)}) && reason=="lane_tangent_invalid", "nonfinite grade cannot enter vertical adapter");
        Check(!Group(new[]{k1},out _,out reason,options:new[]{new P.Choice(k1,-2,0,0,new P.Direction(2,0,.1,true))}) && reason=="lane_tangent_invalid", "non-unit axis fails explicitly");
        Console.WriteLine($"Lane direction policy: {s_Checks} assertions passed.");
    }
}
