namespace NetworkTools.Geometry {
    using System.Collections.Generic;

    // Comparison only. Collection completeness and native rebuild timing belong
    // to the caller; matching values never constitute permission to Apply.
    public static class OriginalInputComparison {
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
