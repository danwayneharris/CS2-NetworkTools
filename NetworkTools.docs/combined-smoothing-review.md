# Combined Smooth Curve + Constant Slope: experimental review

This is a Debug-only, independently opt-in addition to Smooth Curve. It creates
one complete horizontal/vertical candidate and uses one preview and one Apply.
Independent Curve and Slope retain their defaults. Release promotion, Connect
reuse and terrain/obstacle routing are not part of this change.

## Behavior and architecture

The original authored snapshot remains immutable. Curve fitting produces candidate
XZ; vertical fitting recomputes XZ arc lengths and uses original endpoint offsets.
Sectioned profiles preserve outer and explicit split nodes in XYZ. Interior junction
centers keep XZ but participate in the vertical fit; their original vertical grades
are not forced onto the selected path. Ordinary split points
require compatible original vertical grades; incompatible grades reject instead
of inventing a transition policy. Junction attachments have individual constraints.
Topology remains fixed. Incident side-road endpoint/adjacent-handle pairs translate
vertically with moved junctions; both ends are composed when affected. Other
unselected controls remain unchanged. Native validation checks these exact intended
edits and directed connections. See the junction-elevation follow-up session note
for verification of this changed policy; earlier evidence below used fixed junction Y.

Eligible terminal ground ramps reuse the existing restricted surface correction.
Native references are primed for the new horizontal shape; surface and junction
acceptance must belong to the same current revision/submission. Horizontal candidate
search invalidates dependent surface evidence. Pending, failed, stale or changed-
original results cannot enable Apply. Retries/time are bounded. Diagnostics and
provider access remain available independently of the bridge implementation.

Combined mode uses chord-projection stationing. A captured second Apply previously
moved nodes 7.74m horizontally because normalized chord lengths were reused as Bezier
parameters. Production-source replay reproduced that within 0.15mm. The opt-in
stationing correction preserves the longitudinal coordinate on the target, leaving
independent Curve behavior unchanged. Three native combined Applies then differed
by at most 0.34mm from the first result.

See [architecture checkpoint](combined-smoothing-architecture.md) for the original
bounded review, [runtime architecture](system-architecture.md) for implementation,
and [session record](session-notes/2026-10-01-1810-combined-architecture.md) for failures,
commits and incremental evidence. No broad graph-edit framework was introduced.

## Verification ledger

Position/displacement policy: 5cm. Topology, directed lane identity, freshness and
material cumulative drift are separate invariants. Snapshot checks do not certify
vehicle traversal or final rendered road-surface quality.

| Evidence | Result and scope |
| --- | --- |
| Actual offline aggregate at ac974e7 | All seven suites passed: geometry, path selection, parameters, codegen, compiled production Slope/combined adapters, original-input comparison, Python tests. `artifacts/combined-final-offline-ac974e7/summary.json`. |
| Mathematical/adapter cases | Unequal lengths, offsets, invalid/nonfinite inputs, reversal, pins, conflicting/compatible grades, fixed junction anchors, deterministic inputs and atomic rejection. Captured station replay, repeated controls and validator-identity counterexamples run in normal suites. |
| Initial trumpet mainline | Combined preview/Apply passed on the first prototype build; no subsequent broad trumpet certification implied. |
| Hill and crest/dip | Native combined preservation and preview/Apply passed before stationing correction. These recorded results are not asserted to rerun unchanged on every later build. |
| Terminal ramp, stable stationing | Three Applies passed; third maximum node drift 0.25mm/control drift 0.34mm relative to first; outside curves unchanged and directed connections preserved. |
| Reverse terminal ramp | Passed native checks; permanent node/control geometry identical to forward result. |
| Rail branch and interior highway | Passed fixed-position, outside-geometry, topology, directed/physical-lane and preview/Apply checks on stable-station build. |
| Compatible ordinary split | Passed full XYZ preservation, horizontal tangent and vertical-grade continuity, and native checks. Prepared saved crest/dip result used; not mislabeled as unsmoothed. |
| Save/reload | Prepared crest/dip result reloaded with exact captured region fingerprint before split testing. Other independent cases start from checksummed baseline packages. This is not vehicle-route persistence testing. |
| Slope then Curve comparison | Both native stages and the independent basic Slope fit passed from original baseline. |
| Curve then Slope comparison | Both native stages/preservation passed. Additional basic offset-fit prediction FAILED by 5.57m; that predictor excludes surface correction. Not reported as an all-checks-passed comparison. Model qualification remains open. |
| Release compatibility | Compile-only C# target passed on stable-station revision; deployed Debug hash remained unchanged. Not Release postprocessing/Burst/native qualification. Final candidate check recorded in session notes. |
| Ordinary manual UI / human visuals | Not certified by provider automation. Review below is required. |
| Actual vehicles | Not tested in this sprint. |

Debug jobs emit bounded `NetworkTools.CombinedStage` JSON captures for horizontal
and vertical intermediate candidates, keyed by session/submission/mode/stage. The
existing `NetworkTools.SmoothTrace` retains originals and final candidate and alone
updates the native preview probe. Details are omitted above 128 nodes. This logging
does not enable validation or replace native evidence; it is excluded from Release.

Raw captures remain ignored under `artifacts/`. Compact replay fixture
`NetworkTools.Geometry.Tests/Fixtures/combined-repeat-stations.json` preserves the
stationing counterexample in a clean checkout. Source-controlled fixture manifests
record save hashes and positions; local toy saves are not bundled.

## Useful commands and effects

- `scripts/bootstrap.ps1 -OfflineTest`: actual offline aggregate; writes test/build
  outputs but does not deploy or contact the game.
- `scripts/bootstrap.ps1 -Build -Configuration Debug`: full build, postprocess, UI
  and deployment; close the game first.
- `scripts/run-profile-sequence.py --help`: sequence options. `--run` authorizes
  checkpointing, graceful restart/load, preview and Apply on verified toy fixtures.
  Default restarts from baseline; `--use-loaded-baseline` instead requires the
  already loaded geometry to match. Neither skips pause/control/fingerprint checks.
- `--order combined --repeat-combined 3`: measures later results against first,
  stopping on material drift. It does not keep mutating after a failed assertion.
- `NetworkTools.Geometry.Tests --repeat-stations <fixture>` through `dotnet run`:
  offline captured reproduction, no game access.

## Final candidate and game state

Runtime source: 08d7f85 (intermediate diagnostics added after ac974e7; solver unchanged). Final Debug build/postprocess/UI/deploy passed (32 warnings,
zero errors); final Compile-only Release check passed. Deployed DLL SHA256:
`4CC93AB32DEE612674B0647F5A359CCCF4F85CDB50433A56F711C1D0F66427B2`.
Final native three-Apply ramp sequence passed again (0.34mm maximum control drift).
Twelve complete horizontal/vertical/final native stage captures passed the offline
log checker; no XZ changes occurred during vertical/surface stages. All seven
aggregate suites passed again at 08d7f85, and all 19 Python test scripts passed
after adding the diagnostic parser tests.
Evidence: `artifacts/combined-trace-ramp/summary.json` and per-stage reports.
Game paused in the toy loaded from `bridge test - trumpet combined baseline`, with
the final ramp changes saved as
`CitiesIIAgentBridge-combined-review-complete-20261002-025652-37bbd9af`.
No subsequent unsaved network edits were made by this sprint.

## Exact review saves

All are local toy checkpoints and may be loaded independently. Baseline:
`bridge test - trumpet combined baseline` (do not overwrite).

- Ramp after three combined Applies:
  `CitiesIIAgentBridge-combined-review-stable-ramp-repeat-20261002-022720-1f9ce20e`
- Same ramp selected in reverse:
  `CitiesIIAgentBridge-combined-review-ramp-reverse-20261002-023636-db5a5680`
- Rail:
  `CitiesIIAgentBridge-combined-review-rail-20261002-022904-eb9cb8f5`
- Interior highway junctions:
  `CitiesIIAgentBridge-combined-review-interior-highway-20261002-023127-7a36db49`
- Compatible split:
  `CitiesIIAgentBridge-combined-review-split-20261002-023442-c7c2e56e`
- Hill (earlier prototype):
  `CitiesIIAgentBridge-combined-review-hill-road-20261002-021028-eef21927`
- Crest/dip (earlier prototype):
  `CitiesIIAgentBridge-combined-review-crest-dip-20261002-021317-8d62793e`

The final-build ramp checkpoint above is the preferred first visual review.

## Dan's manual checklist

1. On the baseline, open Smooth Curve. Confirm Constant Slope is off initially;
   enable it and inspect Smooth Start/End controls. Strength remains horizontal
   smoothing strength. Independent modes should still behave normally.
2. Inspect the saved ramp from the side and at its highway attachment. Judge the
   rendered slope and continuity; perfect constant rendered grade is not promised.
   Re-select in either direction and Apply twice. No visible walk/drift is expected.
3. Inspect rail and interior-highway review saves. Look for broken tracks, changed
   lane joins, surrounding-road disturbance and preview/Apply disagreement.
4. Inspect the split checkpoint and pin the corresponding ordinary interior point.
   Its XYZ should stay fixed and the join should remain smooth. A different split
   with incompatible original vertical grades may reject with an explanation.
5. Try the unsmoothed trumpet on a separate checkpoint and check the UI's rejected,
   pending and ready states. Test actual vehicle traversal later if desired; lane
   snapshots alone do not establish that behavior.

Remaining decisions: generalized pin-grade transitions, junction-as-user-split
semantics, neighbor-edit tolerances and broader supported surface envelopes remain
future work. Do not infer terrain routing or obstacle avoidance from this prototype.
