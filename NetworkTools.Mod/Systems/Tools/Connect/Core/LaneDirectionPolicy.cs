namespace NetworkTools.Systems.Tools.Connect {
    using System;
    using System.Collections.Generic;

    /// <summary>Pure lane direction/group policy shared by the live adapter and offline tests.</summary>
    public static class LaneDirectionPolicy {
        public readonly struct Point {
            public readonly double X, Y, Z;
            public Point(double x, double y, double z) { X = x; Y = y; Z = z; }
        }
        public readonly struct Direction {
            public readonly double X, Z, Grade;
            public readonly bool Incoming;
            public Direction(double x, double z, double grade, bool incoming) { X = x; Z = z; Grade = grade; Incoming = incoming; }
        }
        public readonly struct Key : IEquatable<Key> {
            public readonly int Index, Version;
            public Key(int index, int version) { Index = index; Version = version; }
            public bool Equals(Key other) => Index == other.Index && Version == other.Version;
            public override bool Equals(object obj) => obj is Key other && Equals(other);
            public override int GetHashCode() => unchecked(Index * 397 ^ Version);
        }
        public readonly struct Choice {
            public readonly Key Identity;
            public readonly double Position;
            public readonly int Carriageway, Group;
            public readonly Direction Direction;
            public readonly string Rejection;
            public Choice(Key identity, double position, int carriageway, int group, Direction direction, string rejection = null) {
                Identity = identity; Position = position; Carriageway = carriageway; Group = group;
                Direction = direction; Rejection = rejection;
            }
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool Finite(Point value) => Finite(value.X) && Finite(value.Y) && Finite(value.Z);

        public static bool TryDirection(bool departure, bool nodeAtStoredEnd, double deltaStart, double deltaEnd,
            bool compositionInverted, Point a, Point b, Point c, Point d, out Direction direction, out string reason) {
            direction = default; reason = null;
            if (!Finite(deltaStart) || !Finite(deltaEnd)) { reason = "lane_source_nonfinite"; return false; }
            var station = nodeAtStoredEnd ? 1.0 : 0.0;
            var atStart = deltaStart == station; var atEnd = deltaEnd == station;
            if (!atStart && !atEnd) { reason = "lane_not_at_endpoint"; return false; }
            if (atStart == atEnd) { reason = "lane_endpoint_ambiguous"; return false; }
            if (atEnd != (nodeAtStoredEnd == !compositionInverted)) {
                reason = "lane_composition_direction_mismatch"; return false;
            }
            // Native already directs a..d along lane travel. New end handle points against travel.
            var x = atEnd ? d.X - c.X : a.X - b.X;
            var y = atEnd ? d.Y - c.Y : a.Y - b.Y;
            var z = atEnd ? d.Z - c.Z : a.Z - b.Z;
            var horizontal = Math.Sqrt(x * x + z * z);
            if (!Finite(a) || !Finite(b) || !Finite(c) || !Finite(d) || !Finite(horizontal) || horizontal <= 1e-5) {
                reason = "lane_tangent_invalid"; return false;
            }
            direction = new Direction(x / horizontal, z / horizontal, y / horizontal, atEnd);
            if (!Finite(direction.Grade)) { reason = "lane_tangent_invalid"; return false; }
            if (departure != atEnd) { reason = "lane_wrong_travel_role"; return false; }
            return true;
        }

        public static bool TryGroup(IReadOnlyList<Choice> choices, IReadOnlyList<double> physicalRoadPositions,
            IReadOnlyList<Key> selected, out Choice representative, out string reason) {
            representative = default; reason = "lane_choice_required";
            if (selected == null || selected.Count == 0) return false;
            var set = new HashSet<Key>();
            foreach (var key in selected) if (!set.Add(key)) { reason = "lane_group_duplicate"; return false; }
            var ordered = new List<Choice>(choices);
            ordered.Sort((a, b) => a.Position.CompareTo(b.Position));
            var identities = new HashSet<Key>();
            for (var i = 0; i < ordered.Count; i++) {
                if (!Finite(ordered[i].Position)) { reason = "lane_source_nonfinite"; return false; }
                if (!identities.Add(ordered[i].Identity)) { reason = "lane_membership_ambiguous"; return false; }
                if (i > 0 && Math.Abs(ordered[i].Position - ordered[i - 1].Position) <= 1e-4) {
                    reason = "lane_lateral_order_ambiguous"; return false;
                }
            }
            var group = new List<Choice>(); var first = -1; var last = -1;
            for (var i = 0; i < ordered.Count; i++) {
                var choice = ordered[i];
                if (!set.Contains(choice.Identity)) continue;
                if (choice.Rejection != null) { reason = choice.Rejection; return false; }
                if (first < 0) first = i;
                last = i; group.Add(choice);
            }
            if (group.Count != selected.Count) { reason = "lane_choice_stale"; return false; }
            if (last - first + 1 != group.Count) { reason = "lane_group_not_contiguous"; return false; }
            foreach (var position in physicalRoadPositions) {
                if (!Finite(position)) { reason = "lane_source_nonfinite"; return false; }
                if (position <= group[0].Position || position >= group[group.Count - 1].Position) continue;
                var included = false;
                foreach (var choice in group) if (choice.Position == position) { included = true; break; }
                if (!included) { reason = "lane_group_not_contiguous"; return false; }
            }
            representative = group[(group.Count - 1) / 2];
            foreach (var choice in group) {
                var direction = choice.Direction;
                if (!Finite(direction.X) || !Finite(direction.Z) || !Finite(direction.Grade)
                    || Math.Abs(direction.X * direction.X + direction.Z * direction.Z - 1.0) > 1e-5) {
                    reason = "lane_tangent_invalid"; representative = default; return false;
                }
                if (choice.Carriageway != representative.Carriageway || choice.Group != representative.Group
                    || direction.Incoming != representative.Direction.Incoming) {
                    reason = "lane_group_incompatible"; representative = default; return false;
                }
                // Maximum horizontal disagreement: one degree. Use a member, never average branches.
                if (direction.X * representative.Direction.X + direction.Z * representative.Direction.Z < Math.Cos(Math.PI / 180)) {
                    reason = "lane_group_direction_conflict"; representative = default; return false;
                }
            }
            reason = null; return true;
        }
    }
}
