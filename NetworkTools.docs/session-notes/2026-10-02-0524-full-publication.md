# Full bounded geometry publication — 2026-10-02 05:24 PDT

Continue from 95a8ec6. Bridge a26e58e adds three final scheduling sites and explicit
Burst-context capture. Eight original scheduling signatures verified offline;
missing/extra sites reject. Deployed only after verified checkpoint
52cc3407b0134edfa019f64b834789aa and graceful close. Exact baseline hash preserved;
new process 73892, city session a5c1e09524104dc4b6b9b9cb3b967e03, Wantagh paused.
NT DLL remains 04792dd (48EF3D8D...636D0AEE9); Bridge deployment hashes retained.

First Burst-enabled preview completed 24 files but exceeded the client's 15-second
deadline (native log preview elapsed 18.56 s). Game heartbeat resumed; recovered
the SAME operation burst-preview-01 and disarmed it. No repeated selection.
Research driver now requests a bounded 45-second command deadline. Ordinary
regression callers retain 15 seconds. Current runtime reports Burst enabled;
this setting alone does not identify which worker executed compiled code.

One entry capture reports nonfinite m_BufferedData. This is the intentionally
uninitialized IntersectionData scratch array: AllocateBuffersJob explicitly calls
ResizeUninitialized(m_Entities.Length), GeometrySystem.cs 1462. CalculateIntersection
constructs a default local value and overwrites each indexed slot (2997); it never
reads prior scratch bytes. The harness reproduces allocation from explicit entity
count and computes all slots. It specifically permits this nonfinite entry-scratch
error, not missing read dependencies, and compares the complete computed exit.
Native record remains unchanged. No recorded scratch output is used as prediction.

Source-derived CalculateIntersectionGeometry, CopyNodeGeometry and UpdateNodeGeometry
compile without algorithm edits. Adapter additions: indexed scratch-list writes;
empty array for explicitly absent chunk components, with unknown presence still
an error. --pipeline-full carries all eight stages and both junction iterations.
First Burst-enabled preview passes: max control error 0.2 mm in edge/finish;
junction, intersection, copy and final node bounds exact in this capture.
Report: artifacts/offline-research/burst-preview-01/pipeline-full-01.json.

Historical failing permanent behavior is not yet validated. Corresponding Apply
capture and full-pipeline negative tests are pending; do not infer goal completion.

**Historical failure recovered.** burst-apply-01 preview/Apply reproduces exactly
1.844445 m on original ramp 54262:1, all five authored curves match, other four
edge surfaces match. The native stage trace reports Burst enabled. Offline
Initialize/CalculateEdge/Flatten (including height-map keys and values) are EXACT
for permanent execution. Finish is the first divergent stage: native retains ramp
end Y=614.5127/613.7286, offline computes 612.6683/611.88416. Later native differences
propagate through the downstream junction and bounds stages. Full replay correctly
returns differential failure, not acceptance. The recorded map contains key
(54262,1) with [612.740356,612.6683,611.9837,611.884155].

This narrows the missing semantics to finishing's map lookup or native execution,
not merely its serialized entries. Finish source 1725–1740 applies a found map value
directly to endpoints; its later middle-height helpers cannot alter end.d. A map
enumeration does not prove TryGetValue can retrieve each entry. Next capture should
retain native bucket/chain structure and actual managed lookup results, while
distinguishing those helper probes from what a Burst worker did. Do not manufacture
a missing-key rule just to match the failing edge.

Twelve full-pipeline validation checks pass on the preview, including poisoned
recorded Copy input scratch, wrong scratch output and wrong final node bounds.
The failing permanent capture is a separate required fixture, not excluded to make
the cohort green. Raw captures and reports are retained locally.

Additional eight-stage captures: burst-control-linear-01 and burst-held-arch-six-01
(24 files each, preview-only). Both also first diverge at Finish, with spatial
component errors up to 2.95547 m and 0.24166 m respectively; downstream errors
propagate. These are useful failing control/variation fixtures, not passing scope.
Native stage counts, hashes and measurements are in the new cohort manifest.

Existing 40 source-world assertions and 16 JSON cases still pass after the chunk
adapter change. Runner's initial timeout override broke one legacy fake-client
test; preserving omitted timeout for ordinary callers fixes it, all 12 regression
driver tests pass. Research calls explicitly request 45 s through the real adapter.

Bridge native-bucket capture addition compiles but is NOT deployed yet; current
runtime still a26e58e. It records heads, reachable slots, links, per-key managed
hash and TryGetValue result using public GetUnsafeBucketData at completed stages.
Need a new checkpoint/graceful restart before its deployment. Current toy remains
paused with the arch preview; trace driver completed and unpatched after capture.
