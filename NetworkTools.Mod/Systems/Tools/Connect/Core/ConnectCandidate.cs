namespace NetworkTools.Systems.Tools.Connect {
    /// <summary>
    /// Connect-owned immutable authored inputs. Lifecycle phase is deliberately not
    /// content identity: Ready-to-Applying must not invalidate the accepted candidate.
    /// TConfig is a value snapshot, never a live parameter object or native container.
    /// Native observation remains owned by the tool's post-rebuild observer.
    /// </summary>
    public sealed class ConnectCandidate<TConfig> where TConfig : struct {
        public readonly long Revision;
        public readonly long Submission;
        public readonly string Inputs;
        public readonly TConfig Config;

        public ConnectCandidate(long revision, long submission, string inputs, TConfig config) {
            Revision = revision;
            Submission = submission;
            Inputs = inputs;
            Config = config;
        }

        // Absence is a stable identity too. Polling an idle/incomplete selection
        // must not consume the public revision token before the next command.
        public static long NextInputRevision(long revision, string previous, string current) =>
            previous == current ? revision : revision + 1;

        public string Status(long revision, long submission, string inputs, bool producerComplete,
            bool updateNeeded, int stableFrames, string observedPreview, string currentPreview, bool nativeAllowsApply) {
            if (Submission <= 0 || submission != Submission || revision != Revision) return "stale_submission";
            if (Inputs == null || inputs == null) return "inputs_unavailable";
            if (Inputs != inputs) return "inputs_changed";
            if (!producerComplete || updateNeeded) return "preview_pending";
            if (stableFrames < 3 || observedPreview == null || currentPreview == null) return "preview_unavailable";
            if (observedPreview != currentPreview) return "preview_changed";
            if (!nativeAllowsApply) return "native_error";
            return "accepted";
        }
    }
}
