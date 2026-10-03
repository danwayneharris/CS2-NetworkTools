using System;
using NetworkTools.Systems.Tools.Connect;
using Port = NetworkTools.Systems.Tools.Connect.LaneConnectionProof.Port;
using Connection = NetworkTools.Systems.Tools.Connect.LaneConnectionProof.Connection;
using Failure = NetworkTools.Systems.Tools.Connect.LaneConnectionProof.Failure;

static class Program {
    static int checks;
    static readonly Port A = new Port(10, 0x401), B = new Port(10, 0x402);
    static readonly Port X = new Port(20, 1), Y = new Port(20, 2);
    static Port Mid(ushort i) => new Port(30, i);
    static Connection Link(Port a, Port b, ushort id = 1) => new Connection(a, Mid(id), b);
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    static void Verify(Port[] selected, Port[] targets, Connection[] graph, bool departure = true,
        Failure expected = Failure.None, int failed = -1) {
        bool success = LaneConnectionProof.Validate(departure,30,10,20,selected,targets,graph,out var failure,out var index);
        Check(success == (expected == Failure.None), "success for " + expected + " actual " + failure);
        Check(failure == expected, "failure expected " + expected + " got " + failure);
        Check(index == failed, "failed selected ordinal");
    }
    static void Main() {
        Verify(new[]{A},new[]{X},new[]{Link(A,X)});
        Verify(new[]{A},new[]{X},new[]{Link(X,A)},false);
        Verify(new[]{A},new[]{X},new[]{Link(X,A)},true,Failure.MissingConnection,0);
        Verify(new[]{A},new[]{X},new[]{Link(A,X)},false,Failure.MissingConnection,0);
        Verify(new[]{A,B},new[]{X,Y},new[]{Link(A,X)},true,Failure.MissingConnection,1);
        Verify(new[]{A,B},new[]{X,Y},new[]{Link(A,X),Link(B,Y,2)});
        Verify(new[]{A,B},new[]{X},new[]{Link(A,X),Link(B,X,2)}); // many-to-one permitted
        Verify(new[]{A},new[]{X,Y},new[]{Link(A,X),Link(A,Y,2)}); // one-to-many permitted
        Verify(new[]{A},new[]{X,Y},new[]{Link(X,A),Link(Y,A,2)},false);
        Verify(new[]{A},new[]{X},Array.Empty<Connection>(),true,Failure.MissingConnection,0);
        Verify(new[]{A},new[]{X},new[]{Link(A,new Port(99,1))},true,Failure.MissingConnection,0);
        Verify(new[]{A},new[]{X},new[]{Link(new Port(99,A.LaneAndSegment),X)},true,Failure.MissingConnection,0);
        Verify(new[]{A},new[]{X},new[]{Link(new Port(10,1),X)},true,Failure.MissingConnection,0); // same lane, wrong segment
        Verify(new[]{A},new[]{X},new[]{Link(new Port(10,A.LaneAndSegment,.5f),X)},true,Failure.MissingConnection,0);
        Verify(new[]{A},new[]{X},new[]{Link(A,new Port(20,X.LaneAndSegment,.5f))},true,Failure.MissingConnection,0);
        Verify(new[]{A},new[]{X},new[]{Link(new Port(10,A.LaneAndSegment,0,true),X)},true,Failure.SecondaryPort);
        Verify(new[]{new Port(10,A.LaneAndSegment,0,true)},new[]{X},new[]{Link(A,X)},true,Failure.SecondaryPort);
        Verify(new[]{A},new[]{new Port(20,1,0,true)},new[]{Link(A,X)},true,Failure.SecondaryPort);
        Verify(new[]{A},new[]{X},new[]{new Connection(A,new Port(30,1,0,true),X)},true,Failure.SecondaryPort);
        Verify(new[]{A},new[]{X},new[]{new Connection(A,new Port(40,1),X)},true,Failure.WrongOwner);
        Verify(new[]{new Port(11,A.LaneAndSegment)},new[]{X},new[]{Link(A,X)},true,Failure.WrongOwner);
        Verify(new[]{A},new[]{new Port(21,1)},new[]{Link(A,X)},true,Failure.WrongOwner);
        Verify(new[]{A,A},new[]{X},new[]{Link(A,X)},true,Failure.DuplicateSelectedPort);
        Verify(new[]{A},new[]{X,X},new[]{Link(A,X)},true,Failure.DuplicateNewPort);
        Verify(new[]{A},new[]{X},new[]{Link(A,X),Link(A,X)},true,Failure.DuplicateJunctionIdentity);
        Verify(new[]{A},new[]{X,Y},new[]{Link(A,X),Link(A,Y)},true,Failure.DuplicateJunctionIdentity);
        Verify(new[]{A},new[]{X},new[]{Link(A,X),Link(A,X,2)},true,Failure.DuplicateConnection);
        // A success witness does not hide later malformed/duplicate evidence.
        Verify(new[]{A},new[]{X},new[]{Link(A,X),Link(B,Y,2),Link(B,Y,3)},true,Failure.DuplicateConnection);
        Verify(new[]{A},new[]{X},new[]{Link(A,X),Link(B,Y,2)}); // unrelated legitimate witness is not exclusivity failure
        // No accidental transitive closure: direct node proof supports no internal-node chains.
        var internalPort = new Port(30,100);
        Verify(new[]{A},new[]{X},new[]{Link(A,internalPort),Link(internalPort,X,2)},true,Failure.MissingConnection,0);
        Verify(null,new[]{X},new[]{Link(A,X)},true,Failure.InvalidInput);
        Verify(Array.Empty<Port>(),new[]{X},new[]{Link(A,X)},true,Failure.InvalidInput);
        Verify(new[]{A},Array.Empty<Port>(),new[]{Link(A,X)},true,Failure.InvalidInput);
        Verify(new[]{A},new[]{X},null,true,Failure.InvalidInput);
        foreach(float station in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1f,1.1f})
            Verify(new[]{new Port(10,A.LaneAndSegment,station)},new[]{X},new[]{Link(A,X)},true,Failure.InvalidInput);
        Verify(new[]{new Port(0,1)},new[]{X},new[]{Link(A,X)},true,Failure.InvalidInput);
        Verify(new Port[65],new[]{X},new[]{Link(A,X)},true,Failure.CapacityExceeded);
        Verify(new[]{A},new Port[1025],new[]{Link(A,X)},true,Failure.CapacityExceeded);
        Verify(new[]{A},new[]{X},new Connection[4097],true,Failure.CapacityExceeded);
        // Permuting native buffers changes neither accepted connectivity nor direction.
        for(ushort i=1;i<=24;i++) {
            var first = Link(A,X,i); var second = Link(B,Y,(ushort)(i+30));
            Verify(new[]{A,B},new[]{X,Y},new[]{first,second});
            Verify(new[]{B,A},new[]{Y,X},new[]{second,first});
            Verify(new[]{A,B},new[]{X,Y},new[]{Link(X,A,i),Link(Y,B,(ushort)(i+30))},false);
        }
        Console.WriteLine("LaneConnectionProof: " + checks + " assertions passed (pure graph proof; ECS/native mapping untested).");
    }
}
