using NetworkTools.Systems.Tools.Connect;

internal struct Config {
    public double StartHandle;
    public int PrefabIndex;
    public int PrefabVersion;
}

internal static class Program {
    private static int s_Checks;
    private static void Check(bool condition, string message) {
        ++s_Checks;
        if (!condition) throw new Exception(message);
    }

    private static void Main() {
        var liveConfig = new Config { StartHandle = 12, PrefabIndex = 43, PrefabVersion = 2 };
        var candidate = new ConnectCandidate<Config>(7, 11, "endpoints/curves/prefab A", liveConfig);
        string Status(long revision = 7, long submission = 11, string inputs = "endpoints/curves/prefab A",
            bool complete = true, bool dirty = false, int stable = 3, string observed = "preview A",
            string current = "preview A", bool nativeAllowed = true) => candidate.Status(
                revision, submission, inputs, complete, dirty, stable, observed, current, nativeAllowed);

        Check(Status() == "accepted", "matching finished candidate is eligible");
        // Request-to-execution handoff changes lifecycle phase, not operation content.
        // Calling the exact production policy a second time must remain eligible.
        var acceptedRequest = candidate;
        Check(acceptedRequest.Status(7, 11, "endpoints/curves/prefab A", true, false, 3,
            "preview A", "preview A", true) == "accepted", "accepted request remains valid during execution");

        Check(Status(complete: false) == "preview_pending", "producer job must finish even when preview text matches");
        Check(Status(dirty: true) == "preview_pending", "parameter write awaiting rebuild cannot use old preview");
        Check(Status(stable: 2) == "preview_unavailable", "insufficient consecutive observations");
        Check(Status(observed: null) == "preview_unavailable", "missing accepted native observation");
        Check(Status(current: null) == "preview_unavailable", "native temp disappearance rejects");
        Check(Status(current: "preview B") == "preview_changed", "changed native result after request rejects");
        Check(Status(nativeAllowed: false) == "native_error", "native error cannot be bypassed by provider");
        Check(Status(inputs: null) == "inputs_unavailable", "missing endpoint/component context rejects");
        foreach (var changed in new[] {
            "changed endpoint version", "changed incident curve", "changed handle",
            "changed node structural elevation", "changed effective prefab", "changed validation setting"
        }) {
            Check(Status(inputs: changed) == "inputs_changed", "request-to-execution content change: " + changed);
        }
        Check(Status(revision: 8) == "stale_submission", "old revision rejects even after content restored");
        Check(Status(submission: 12) == "stale_submission", "new preview submission invalidates prior acceptance");
        var absent = new ConnectCandidate<Config>(0, 0, null, default);
        Check(absent.Status(0, 0, null, true, false, 9, "x", "x", true) != "accepted",
            "uninitialized candidate never passes");
        var unavailable = new ConnectCandidate<Config>(7, 11, null, liveConfig);
        Check(unavailable.Status(7, 11, "A", true, false, 3, "x", "x", true) == "inputs_unavailable",
            "missing submitted baseline never certifies current inputs");

        // The accepted object owns a struct copy, not the live parameter storage.
        liveConfig.StartHandle = 999;
        liveConfig.PrefabIndex = 99;
        liveConfig.PrefabVersion = 8;
        Check(acceptedRequest.Config.StartHandle == 12, "accepted authored handle remains captured");
        Check(acceptedRequest.Config.PrefabIndex == 43 && acceptedRequest.Config.PrefabVersion == 2,
            "accepted resolved prefab identity remains captured");
        var copiedConfig = acceptedRequest.Config;
        copiedConfig.StartHandle = -1;
        Check(acceptedRequest.Config.StartHandle == 12, "consumer receives a value copy");
        Check(Status(inputs: "changed handle") != "accepted", "immutable config does not license stale world application");
        Check(Status() == "accepted", "pure evaluation is deterministic and has no hidden acceptance mutation");
        // Actual input-revision helper used by provider state refresh. An idle
        // state token must survive another state refresh inside clear/select.
        long idleRevision = 0;
        for (var poll = 0; poll < 4; poll++)
            idleRevision = ConnectCandidate<Config>.NextInputRevision(idleRevision, null, null);
        Check(idleRevision == 0, "repeated idle polling preserves revision");
        var selectToken = idleRevision;
        var commandRevision = ConnectCandidate<Config>.NextInputRevision(idleRevision, null, null);
        Check(commandRevision == selectToken, "select precondition still matches after internal state refresh");
        var selectedRevision = ConnectCandidate<Config>.NextInputRevision(commandRevision, null, "selection A");
        Check(selectedRevision == selectToken + 1, "completed selection advances revision once");
        Check(ConnectCandidate<Config>.NextInputRevision(selectedRevision, "selection A", "selection A") == selectedRevision,
            "unchanged selected context does not advance revision");
        var selected = new ConnectCandidate<Config>(selectedRevision, 1, "selection A", default);
        var lostRevision = ConnectCandidate<Config>.NextInputRevision(selectedRevision, "selection A", null);
        Check(lostRevision == selectedRevision + 1, "context loss invalidates old accepted revision");
        Check(ConnectCandidate<Config>.NextInputRevision(lostRevision, null, null) == lostRevision,
            "repeated unavailable context stays stable so clear remains possible");
        Check(selected.Status(lostRevision, 1, null, true, false, 9, "preview", "preview", true) != "accepted",
            "stable missing identity does not make unavailable context ready");
        var restoredRevision = ConnectCandidate<Config>.NextInputRevision(lostRevision, null, "selection A");
        Check(restoredRevision == lostRevision + 1, "restored context advances revision again");
        Check(selected.Status(restoredRevision, 1, "selection A", true, false, 9, "preview", "preview", true) == "stale_submission",
            "restoration cannot resurrect an obsolete candidate");
        Console.WriteLine($"Connect candidate production-source tests passed: {s_Checks} assertions.");
    }
}
