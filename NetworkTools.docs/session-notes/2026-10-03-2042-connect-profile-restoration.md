# Connect authored profile restoration — 2026-10-03 20:42

Task: correct PR #20's sloped Connect rejection without weakening acceptance,
and extend replay coverage of the previously missing course-height stage.

The captured native subdivision preserves XZ and shared outer endpoint heights,
but resamples Y controls from course-height data. An exact authored subdivision
is therefore recoverable before node generation. Added an atomic construction
helper that requires unique, complete horizontal coverage and preserves native
XZ. This is separate from, and does not replace, the strict native-result oracle.

Integration requirements: explicit definition ownership; no moving existing
interior connections; elevation/bridge/tunnel classification must be recomputed
or verified unchanged. Merely editing finished curves or relaxing the height
tolerance is insufficient.

Implemented a Debug placement adapter around native CourseSplit: snapshot tagged
Connect-only definition batches before splitting; restore complete horizontal
subdivisions after ToolReadyBarrier and before GenerateNodes. Existing entity
heights must match. Auxiliary/fixed/service networks, mixed batches, replacements,
upgrades, water/shoreline and bridge/tunnel transitions refuse restoration. Both
road edges at native classification stations must remain within ground limits;
zero elevation metadata is then unchanged. No permanent edge is directly edited.
The same emitted marker participates in preview and Apply; reset of the tool does
not erase the pending batch. Native-result validation remains unchanged.

Offline: production Debug compilation and compiled Slope tests passed. Captured
restoration: 59 assertions passed; original coverage: 190. Initial fixture export
had a flattened outer JSON array (test failed before assertions); fixed the export
shape, no production tolerance changes. Added pinned original SampleCourseHeight
and SampleHeight bodies to NativeReplay, with explicit managed height samples.
All six method tests passed. This does not replay terrain-buffer construction,
intersection search, classification or a complete native course pipeline.

Runtime verification pending at this commit. No deployment performed yet.

## First live experiment and revised classification

Deployed 22920bc and loaded the checksummed `bridge test - connect repro` baseline.
The adapter ran at the intended boundary but refused the ground-only envelope.
Inspection found four native definitions with asymmetric nonzero elevation values
and RightTransition flags on new interior courses. The apparent ordinary road
connection already exercises elevated/cut classification. No Apply was attempted.

Revised the adapter to recompute classification at A/mid/D, on both road edges,
using restored heights, native terrain sampling, prefab thresholds and placement
clamps. New, unconnected interior transition flags belong to the old sampled
profile and are cleared; flags at existing endpoints remain. Forced structures,
auxiliary/fixed/service/owned/shoreline cases remain outside scope. Splits and XZ
remain native; all writes are staged until the whole batch qualifies.

Added original CalculateElevation/LimitElevation bodies to the local generated
replay, using the existing checked terrain adapter. 34 native-method checks pass,
including 28 classifier comparisons over signed limits, prefab clamps and side
transitions. Course restoration now has 65 assertions. Non-deploying production
compile and Slope tests passed. Broader offline aggregate passed on 22920bc.
Second live experiment remains pending.

## Second live experiment: passed for Dan's repro

Deployed clean Debug `02b899b89f9c777875db947b531fcd4fae9e44a5`;
81 manifest artifacts verified. DLL SHA256
`59088111724B490253FD52801934B006CE6A66559908A6D75DB8B7D5D8EA01D8`.
Reloaded the same checksummed `bridge test - connect repro` baseline, identified
fresh endpoint entities by position and checkpointed before changing the tool.
Profile preview reported `restored_4`, `accepted`, `previewReady=true`.
Apply completed; all four new permanent cubics matched preview exactly (0 m
maximum control error). Independent existing-node/edge preservation and prefab
checks passed; physical adjacency was true. A later settled readback passed again.

Final ten-suite offline aggregate passed. Regenerated native-method harness passed
34 checks. Replay validation excludes full ECS splitting/height-buffer construction;
vehicle/lane identity, visual surfaces, broad prefabs and save/reload remain unverified.
No other segment modification option was needed for this fix.

Checkpoints (baseline never overwritten):
- Initial preservation: `CitiesIIAgentBridge-regression-before-reload-20261004-035530-91177ecb`
- Before revised test: `CitiesIIAgentBridge-connect-course-restoration-before-20261004-040812-9a5aca13`
- Final review result: `CitiesIIAgentBridge-review-connect-profile-restored-20261004-041300-993c81a7`

Game remains paused, city session `65f9433f8b72494b955f205e69dedc54`, with the four
new connection edges saved in the review checkpoint. No further unsaved geometry
changes after that save. Detailed mechanics, limitations and Dan's review:
[course-height replay](../connect-course-height-replay.md).
