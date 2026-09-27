namespace NetworkTools.Geometry {
    /// <summary>First failed precondition; index identifies a node/edge when applicable.</summary>
    public enum SmoothFailure {
        None, InvalidArguments, PathCountMismatch, NonFiniteNode, DegenerateNodeChord,
        NonFiniteCurve, PathLengthOverflow, DegenerateBoundaryChord, InteriorPinnedNode,
        BackwardNodeChord, BoundaryTangents, InvalidSlice, RepeatedNode, FloatOverflow
    }
}
