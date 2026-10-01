namespace NetworkTools.Geometry {
    using System.Collections.Generic;

    // Candidate ordering only: a remembered angle is never proof of connectivity.
    public sealed class JunctionCandidateSearch {
        private List<object> m_AcceptedKey;
        private List<object> m_CurrentKey;
        private int m_AcceptedDegrees;
        private readonly List<int> m_Candidates = new();
        public int Attempt { get; private set; }
        public int Degrees => m_Candidates[Attempt];
        public bool WarmStart { get; private set; }

        public void Reset(IReadOnlyList<object> key, bool zeroStrength) {
            m_CurrentKey = key == null ? null : new List<object>(key);
            m_Candidates.Clear();
            WarmStart = !zeroStrength && OriginalInputComparison.Compare(m_AcceptedKey, key) == "matches";
            if (WarmStart) m_Candidates.Add(m_AcceptedDegrees);
            Add(0);
            if (!zeroStrength) for (var angle = 1; angle <= 15; angle++) { Add(angle); Add(-angle); }
            Attempt = 0;
        }
        private void Add(int degrees) { if (!m_Candidates.Contains(degrees)) m_Candidates.Add(degrees); }
        public bool Advance() {
            if (Attempt + 1 >= m_Candidates.Count) return false;
            Attempt++;
            return true;
        }
        public void RememberAccepted() {
            m_AcceptedKey = m_CurrentKey == null ? null : new List<object>(m_CurrentKey);
            m_AcceptedDegrees = Degrees;
        }
    }

    // Repeated sets are meaningful only after the caller's native freshness checks.
    public sealed class StableSetObservation<T> {
        private HashSet<T> m_Last;
        private int m_Count;
        public void Reset() { m_Last = null; m_Count = 0; }
        public bool Observe(HashSet<T> current) {
            if (current == null) { Reset(); return false; }
            if (m_Last == null || !m_Last.SetEquals(current)) {
                m_Last = new HashSet<T>(current);
                m_Count = 1;
            } else m_Count++;
            return m_Count >= 3;
        }
    }
}
