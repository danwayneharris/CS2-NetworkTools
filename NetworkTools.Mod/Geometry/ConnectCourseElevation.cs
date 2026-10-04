namespace NetworkTools.Geometry {
    /// <summary>
    /// Ordinary unowned course classification, matching the threshold and placement
    /// clamps in CourseSplitSystem.CalculateElevation/LimitElevation. Terrain and
    /// water sampling, forced structures, auxiliary networks and ownership belong
    /// to the adapter, not this scalar helper.
    /// </summary>
    public static class ConnectCourseElevation {
        public static float Classify(float relativeHeight, float limit, float minimum, float maximum, bool transition) {
            var value = transition || (relativeHeight < limit && relativeHeight > -limit) ? 0f : relativeHeight;
            if (minimum >= 0f && value < minimum) value = minimum;
            if (maximum < 0f && value > maximum) value = maximum;
            return value;
        }
    }
}
