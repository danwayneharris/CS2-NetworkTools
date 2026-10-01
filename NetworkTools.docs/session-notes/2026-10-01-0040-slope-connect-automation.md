# Slope and Connect automation — October 1, 2026

Authorized exposing both tools through NetworkTools' existing generic provider.
Worktree dan/terrain-regressions is six analysis commits above latest origin/main
4b5fc1c (fresh fetch checked), clean before this work. Bridge remains independent.
Retain Curve command compatibility. Add Slope with matched current native preview
and original-input checks. Connect creates new edges and requires its own preview
correlation rather than reusing original-edge matching. No blanket Apply bypass.

Initial non-deploying Debug compile found ambiguous SubLane imports (Net versus
Prefabs). Qualified Game.Net.SubLane and compile passed. Added explicit Unity
vector serialization to avoid recursively reflecting swizzle properties in Connect
snapshots. Runtime verification pending. Bridge branch audit found identical
feature/main trees after squash; fetched and switched clean bridge to latest main
8843041. No bridge source changes. Review blocked directory-wide staging; use
explicit reviewed paths for the local commit.

## Live outcomes

Full Debug build/postprocess/UI/deploy passed. Existing Curve off-ramp fixture
passed from original v1.1. Controlled Curve -> Slope ease attempt blocked before
Apply: native probe matched all seven originals but two curves differ from job
output. Inputs/revision match and buffers exist. Do not loosen the guard; retain
this as the next integration investigation. No Slope mutation occurred there.

Connect SimpleCurve between hill-road end and crest/dip start passed: two new
permanent edges and one node; physical adjacency established and new curve controls
match observed native preview exactly (0 error). Offline comparison rejects an
injected 10 cm handle error. This does not certify vehicle traversal or lanes.
Connect used existing Anarchy=true, untouched by automation.

Slope ease then passed on the hill road (after the Connect trial): exact native
preview/permanent curves, topology preserved and no unselected curve changes.
Both new command groups rejected old revision tokens. State snapshots and full
requests retained under provider-tools-connect and provider-tools-slope-hill.
Eight existing regression guard tests pass. Non-deploying Release Compile target
passes, not Release postprocess/Burst/runtime verification. Other modes and Connect
parameter-change/native-entity-reuse cases remain untested.

Bootstrap rewrote optional npm platform entries. Kept a complete backup capture
before restoring the build-generated change in this initially clean worktree.
The original NT checkout's unrelated lockfile edit remains untouched.

New provider APIs are implemented and the above paths are exercised, but do not
call the guarded off-ramp slope issue fixed. Next: capture requested versus native
Slope curves at the two mismatched edges, trace junction/endpoint adjustment, then
verify Apply agreement with a tested correction rather than disabling readiness.

Final paused checkpoint: CitiesIIAgentBridge-slope-connect-tested-20261001-074741-67590afa.cok.
Loaded from original terrain v1.1, now contains Curve-applied off-ramp, the new
hill-to-crest/dip connection, and hill-road Slope ease Apply. All saved at checkpoint;
no further network changes. Original baseline and user curve/slope saves preserved.
