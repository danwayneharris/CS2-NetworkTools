# Live regression runner

`scripts/live-regression.py` is a development harness, not a mod feature or a
complete test oracle. It records every request and response and never blindly
retries a timed-out mutation. It must run against the local Debug NT adapter and
matching bridge, in the reduced playset, with a paused toy save.

Read-only discovery:

```powershell
python scripts/live-regression.py --fixture scripts/fixtures/toy-networks.json --output <new-capture-folder>
```

An explicitly authorized mutation run:

```powershell
$saves = Join-Path ([Environment]::GetEnvironmentVariable('CSII_USERDATAPATH','User')) Saves
python scripts/live-regression.py --fixture scripts/fixtures/toy-networks.json --case rail-merge --run --save-root $saves --output <new-capture-folder>
```

Reload the fixture's named baseline before each case. The committed fixture
records its checksum and local geometry fingerprint, not reusable entity IDs.
The save itself remains local. Nodes resolve from unique positions after each
load. A geometry mismatch aborts instead of silently updating expected results.

Cases currently described: rail merge, four-way road branch, highway on ramp and
off ramp. These are definitions, not assertions that all four have passed.

Before editing the runner checks the adapter, pause, empty-city population,
unchanged bridge city session, baseline package checksum and geometry. It creates
a uniquely named checkpoint, polls its result and checks the ZIP/package metadata.
It then selects, sweeps strength, captures selected-edge previews through adjacent
node snapshots, rechecks the preview token, and applies exactly that submission.

Post-Apply checks cover captured node/edge identities, edge endpoints and prefabs,
node elevations, fixed/unselected node positions, unselected edge curves, selected
preview/permanent control points and watched directed car/track lane pairs. Exact
pair-set comparisons catch lost and added connections even if counts are unchanged.

Limitations: bounded query region; far nodes of long edges can fall outside it.
No traffic traversal, visual lane-marking assessment, formal native rebuild fence,
or automatic proof of the active playset. Snapshot-local lane identity is not a
full route-permission model. Single-edge isolated paths have insufficient preview
coverage through the current shared-node resolver and are explicitly rejected.
Current lifecycle orchestration still requires a preceding known baseline load.

First live attempt stopped before selecting because the local mod was disabled;
the checkpoint succeeded and no smoothing was applied. See the sprint session
note. Run `python scripts/test-live-regression.py` for offline guard tests; those
tests do not establish in-game correctness.

The separate `scripts/fixtures/toy-highway-jank.json` describes the user's newer
highway-jank toy save. Its rail-merge case passed on the milestone deployment on
2026-09-29: three selected edges changed, preview/permanent curves matched and
watched directed lane-pair sets were preserved. Road-four-way-branch and highway
on/off-ramp cases also passed on that deployment.
Optional `splits` contain pinned node coordinates; their post-Apply checks include
fixed positions and planar tangent agreement. Two-pin rail Apply passed at 0,
0.5 and 1.0 on the split deployment. The one-pin case retains a strict-test failure
for 3 mm outer junction-center drift, independently reproduced offline.

Lane verification follows directed transitions between incident composition lanes,
including direct joins represented by shared PathNodes without connector lanes.
It compares physical lane composition mappings too (lateral positions, direction,
carriageway, prefab, width), and retains raw connector changes as diagnostics.
`test-lane-connectivity.py` exercises recorded direct joins plus deliberately broken
connections and lane swaps. This does not establish vehicle traversal or all
traffic/access permissions. `replay-regression.py` can rerun assertions on captures
without contacting the game; `replay-node-center.py` is a bounded alignment model.

Use `scripts/reload-toy-baseline.py --fixture <fixture> --save-root <Saves>
--expected-city-session <observed-toy-session> --output <new-directory>` for a
checkpointed graceful restart to the fixture baseline. It never force-kills or
changes playsets. Supply only a session independently identified as the toy city.
Check readiness afterward; successful launch is not successful loading.

Post-Apply now requires three matching permanent observations with no observed
Updated/Created owners. This is stability evidence, not a native completion fence.
The newer baseline's road-four-way-branch also passed on the milestone deployment;
see the session note for exact coverage and lane-identity limitations.


## Generic provider transport and suite orchestration

The current runner uses the bridge's standard-library Python client and discovers
`networktools` through the generic provider catalog. It requires `active` and
`smoothMode` before issuing selection commands. Both process and city identities
are checked before publication. Intents and responses are retained; uncertain
mutations are never automatically resubmitted.

`run-provider-suite.py --run --save-root <Saves> --output <new-folder>
--case toy-highway-jank.json:road-four-way-branch` wraps checkpointed graceful
reloads and existing assertions. Repeat --case for more fixtures. It never
force-kills the game. An incomplete case aborts; a completed assertion failure is
recorded and the next case starts from its baseline. The tool is for explicitly
authorized toy sessions, not arbitrary player saves.

`live-mcp-smoke.py` is a consumer-owned read-only check using the bridge adapter's
Python environment. It verifies actual stdio discovery/state, not Apply or routing.
Current generic captures are supported by replay-regression.py. Historical captures
without smoothMode require their historical runner to reproduce historical checks;
they cannot prove the new explicit mode-readiness predicate.
