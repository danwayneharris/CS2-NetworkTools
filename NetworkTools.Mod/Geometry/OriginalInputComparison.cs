namespace NetworkTools.Geometry {
    using System.Collections.Generic;

    // Comparison only. Collection completeness and native rebuild timing belong
    // to the caller; matching values never constitute permission to Apply.
    public static class OriginalInputComparison {
        public static string Observe(long currentRevision, long submittedRevision,
            IReadOnlyList<object> submitted, IReadOnlyList<object> current) {
            if (currentRevision <= 0 || submittedRevision != currentRevision) { return "stale_revision"; }
            return Compare(submitted, current);
        }
        // The calculation cache, submitted baseline, and present world must all
        // describe the same values. Matching only submitted/current misses A->B
        // edits that happened before submission while the fit still used A.
        public static string CandidateStatus(long revision, long submittedRevision,
            IReadOnlyList<object> cached, IReadOnlyList<object> submitted, IReadOnlyList<object> current) {
            if (revision <= 0 || revision != submittedRevision) return "stale_revision";
            var cacheStatus = Compare(cached, submitted);
            return cacheStatus == "matches" ? Compare(submitted, current) : cacheStatus;
        }

        public static string Compare(IReadOnlyList<object> submitted, IReadOnlyList<object> current) {
            if (submitted == null || current == null) { return "unavailable"; }
            if (submitted.Count != current.Count) { return "changed"; }
            for (var i = 0; i < current.Count; i++) {
                if (!object.Equals(current[i], submitted[i])) { return "changed"; }
            }
            return "matches";
        }
    }
}
