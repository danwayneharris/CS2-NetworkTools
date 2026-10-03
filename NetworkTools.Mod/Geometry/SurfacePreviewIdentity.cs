namespace NetworkTools.Geometry {
    /// <summary>Fresh surface acceptance must belong to the same complete candidate as native verification.</summary>
    public static class SurfacePreviewIdentity {
        public static bool AllowsApply(long inputRevision, long surfaceRevision,
            int submission, int observedSubmission, int verifiedSubmission,
            bool accepted, bool failed) =>
            surfaceRevision == inputRevision && accepted && !failed
            && observedSubmission == submission && verifiedSubmission == submission;
    }
}
