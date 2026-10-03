namespace NetworkTools.Systems.Tools.Connect {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Pure direct-junction existence proof. The adapter supplies current, permitted
    /// native lanes after checking entity versions, Temp.original, ownership, composition and endpoint travel role.
    /// This proves each selected port has at least one directed witness to/from the
    /// new edge. It does not prove exclusivity, cross-junction routing or traffic flow.
    /// </summary>
    public static class LaneConnectionProof {
        public readonly struct Port : IEquatable<Port> {
            public readonly int Owner;
            public readonly ushort LaneAndSegment;
            public readonly float CurvePosition;
            public readonly bool Secondary;
            // Values come directly from PathNode getters; no station rounding or
            // lane-index mask is permitted here. Owner is an index, as in PathNode;
            // the ECS adapter must independently verify live full Entity identities.
            public Port(int owner, ushort laneAndSegment, float curvePosition = 0, bool secondary = false) {
                Owner = owner; LaneAndSegment = laneAndSegment; CurvePosition = curvePosition; Secondary = secondary;
            }
            public bool Equals(Port other) => Owner == other.Owner && LaneAndSegment == other.LaneAndSegment
                && CurvePosition.Equals(other.CurvePosition) && Secondary == other.Secondary;
            public override bool Equals(object other) => other is Port port && Equals(port);
            public override int GetHashCode() {
                unchecked { return ((Owner * 397 ^ LaneAndSegment) * 397 ^ CurvePosition.GetHashCode()) * 397 ^ Secondary.GetHashCode(); }
            }
            internal bool Valid => Owner > 0 && !float.IsNaN(CurvePosition) && CurvePosition >= 0 && CurvePosition <= 1;
        }
        public readonly struct Connection {
            public readonly Port Start, Middle, End;
            public Connection(Port start, Port middle, Port end) { Start = start; Middle = middle; End = end; }
        }
        public enum Failure {
            None, InvalidInput, CapacityExceeded, WrongOwner, SecondaryPort,
            DuplicateSelectedPort, DuplicateNewPort, DuplicateJunctionIdentity, DuplicateConnection, MissingConnection
        }

        public static bool Validate(bool departure, int nodeOwner, int approachOwner, int newEdgeOwner,
            IReadOnlyList<Port> selected, IReadOnlyList<Port> newPorts, IReadOnlyList<Connection> junctions,
            out Failure failure, out int failedSelected) {
            failedSelected = -1; failure = Failure.InvalidInput;
            if (nodeOwner <= 0 || approachOwner <= 0 || newEdgeOwner <= 0 || nodeOwner == approachOwner
                || nodeOwner == newEdgeOwner || approachOwner == newEdgeOwner
                || selected == null || newPorts == null || junctions == null || selected.Count == 0 || newPorts.Count == 0) return false;
            if (selected.Count > 64 || newPorts.Count > 1024 || junctions.Count > 4096) {
                failure = Failure.CapacityExceeded; return false;
            }
            var selectedSet = new HashSet<Port>(); var newSet = new HashSet<Port>();
            for (int group = 0; group < 2; group++) {
                var ports = group == 0 ? selected : newPorts;
                var set = group == 0 ? selectedSet : newSet;
                int owner = group == 0 ? approachOwner : newEdgeOwner;
                foreach (var port in ports) {
                    if (!port.Valid) { failure = Failure.InvalidInput; return false; }
                    if (port.Owner != owner && port.Owner != nodeOwner) { failure = Failure.WrongOwner; return false; }
                    if (port.Secondary) { failure = Failure.SecondaryPort; return false; }
                    if (!set.Add(port)) { failure = group == 0 ? Failure.DuplicateSelectedPort : Failure.DuplicateNewPort; return false; }
                }
            }
            var identities = new HashSet<Port>();
            var connections = new HashSet<Tuple<Port, Port>>();
            var witnessed = new HashSet<Port>();
            // LaneReferencesSystem (Game 1.6.2f1):163-176 maps skipped junction
            // lane start/middle/end to one node-owned key; :237-307 rewrites the
            // incident edge ports to that key. Exact shared-node identity is then
            // direct continuity, even though no NodeLane entity remains between them.
            // The adapter MUST supply incoming/outgoing boundary ports for the
            // requested role, from distinct verified approach/new-edge lane entities.
            // Never accept same lane number, owner-blind equality or an edge-owned
            // shared port as this shortcut. Inspect ALL evidence below before success.
            foreach (var port in selected) {
                if (port.Owner == nodeOwner && newSet.Contains(port)) witnessed.Add(port);
            }
            foreach (var junction in junctions) {
                if (!junction.Start.Valid || !junction.Middle.Valid || !junction.End.Valid) { failure = Failure.InvalidInput; return false; }
                if (junction.Middle.Owner != nodeOwner) { failure = Failure.WrongOwner; return false; }
                if (junction.Start.Secondary || junction.Middle.Secondary || junction.End.Secondary) {
                    failure = Failure.SecondaryPort; return false;
                }
                if (!identities.Add(junction.Middle)) { failure = Failure.DuplicateJunctionIdentity; return false; }
                if (!connections.Add(Tuple.Create(junction.Start, junction.End))) { failure = Failure.DuplicateConnection; return false; }
                var source = departure ? junction.Start : junction.End;
                var target = departure ? junction.End : junction.Start;
                if (selectedSet.Contains(source) && newSet.Contains(target)) witnessed.Add(source);
            }
            for (int i = 0; i < selected.Count; i++) if (!witnessed.Contains(selected[i])) {
                failedSelected = i; failure = Failure.MissingConnection; return false;
            }
            failure = Failure.None; return true;
        }
    }
}

