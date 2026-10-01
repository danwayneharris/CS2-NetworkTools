namespace NetworkTools.Geometry {
    using System;

    /// <summary>Bounded candidate convergence; independent of frame timing and native freshness.</summary>
    public sealed class SurfacePreviewConvergence {
        public enum Result { Retry, Accepted, Failed }
        private int m_Attempts, m_SmallChanges;
        private bool m_HadCorrection;
        public Result Observe(bool corrected, double displacement) {
            ++m_Attempts;
            if (!corrected) { return m_HadCorrection ? Result.Failed : Result.Accepted; }
            m_HadCorrection = true;
            if (double.IsNaN(displacement) || displacement < 0) { return Result.Failed; }
            m_SmallChanges = displacement <= .05 ? m_SmallChanges + 1 : 0;
            if (m_SmallChanges >= 2) { return Result.Accepted; }
            return m_Attempts >= 6 ? Result.Failed : Result.Retry;
        }
    }
}
