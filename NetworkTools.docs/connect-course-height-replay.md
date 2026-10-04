# Connect course heights: correction and replay

Implemented on PR #20's branch, Debug only. Native repro verified with deployed
`02b899b89f9c` on October 3, 2026. This extends the supported profile envelope;
it does not qualify general terrain routing, crossings, or auxiliary networks.

## What the game does

Installed Game.dll SHA256:
`AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A`.
Local `Game.Tools/CourseSplitSystem.cs` provides the source evidence:

- `CourseHeightData` (around 1343) constructs a terrain-derived height buffer for
  ordinary unowned courses. Fixed outer positions do not fix the whole profile.
- `SampleCourseHeight` (1679) cuts the XZ cubic, substitutes endpoint heights,
  samples heights at interior thirds, and reconstructs vertical controls.
- `CalculateElevation` (3588/3636) independently derives left/right structural
  elevation at start, midpoint and end. Classification uses road-width offsets,
  threshold comparisons, transition flags and prefab placement clamps.
- `AddCourse` (3781) copies selected native components to new split definitions;
  custom ownership tags are not propagated. Course positions can have horizontal
  offsets from Bézier endpoints; the correction preserves those offsets.

In the captured case all outer/internal height joins already agreed. Native XZ
control error was below 0.4 mm, but vertical control differences reached 3.39 m.
Those are **control-polygon differences**, not measured rendered-surface error.
Fixing a shared endpoint height alone therefore would not solve this rejection.

## Correction boundary

Connect tags its own authored definitions. A pre-split system snapshots an
exclusive one/two-course batch. After ToolReadyBarrier, before GenerateNodes,
the restoration system requires complete, unique horizontal subdivision coverage.
It restores exact authored vertical subcurves while retaining native XZ, splitting,
and node-position offsets. No existing road or node is directly modified.

The entire batch is staged before writing. Existing connection heights must
remain within 5 cm. Native-generated transition flags at new internal positions
are removed because they describe the discarded sampled profile; existing endpoint
transition policy is retained. Structural elevation is recalculated from restored
height and current terrain using the qualified ordinary-course rules.

Mixed definition batches, replacements, owned/auxiliary/fixed/service networks,
forced structures, shoreline/water cases and ambiguous horizontal mapping refuse
restoration. Unsupported results remain subject to the unchanged strict native
profile validator. Preview and Apply both emit the same authored markers, using
the frozen accepted candidate. `connect_state.profileRestoration` reports the last
processed restoration outcome; it is diagnostic, not an acceptance token.

## Reproduce the offline evidence

From the repo root (no deployment or game access):

```powershell
dotnet run --project NetworkTools.ConnectCoverage.Tests
python scripts/generate-native-world-stages.py --decompile <verified-local-source>
$managed = [Environment]::GetEnvironmentVariable('CSII_MANAGEDPATH','User')
dotnet build NetworkTools.NativeReplay "-p:ManagedPath=$managed"
dotnet NetworkTools.NativeReplay/bin/Debug/net8.0/NetworkTools.NativeReplay.dll --course-height-tests <new-report.json>
```

ConnectCoverage uses a compact captured fixture: 65 restoration/classification
assertions plus the existing 190 coverage assertions. Missing, duplicate, changed,
reversed and nonfinite inputs are exercised; reconstruction is atomic and
idempotent. Strict validation still rejects a subsequent 5.1 cm corruption.

NativeReplay executes hash-pinned original height-sampling and classification
method bodies against explicit managed buffers/terrain. 34 checks passed,
including 28 scalar classifier comparisons against native CalculateElevation.
Proprietary generated code remains ignored/local. This does **not** replay the
height-buffer constructor, intersection discovery, full splitting, or ECS scheduling.
See [capabilities](../NetworkTools.NativeReplay/capabilities.json).

## Live evidence and limitations

Checksummed baseline: `bridge test - connect repro`. The first ground-only adapter
refused correctly: the game had generated elevated/asymmetric transitions. The
revised classifier restored four courses and native preview accepted. Apply matched
all four preview cubics exactly (one-to-one comparison, maximum error 0 m).
Existing node positions, existing edge curves/topology and prefab identity were
preserved. Physical adjacency was confirmed independently.

Reusable `scripts/replay-connect-course-case.py` requires an explicit current city
session, a checksummed baseline fixture and save root. Without `--run` it reads;
`--run` checkpoints and changes preview; `--apply` additionally applies once and
checks permanent results. Never retry an uncertain Apply. Raw evidence is local
under `artifacts/connect-profile-native-v2`; a compact summary is in session notes.

This was an Anarchy-enabled toy session. No collision, directed lane, traffic,
save/reload, broad prefab, Release/Burst or surface/visual certification is implied.
The current correction retains native segmentation chosen before restoration;
general crossing discovery and structural-transition routing need separate work.

## Dan's review

Inspect the newly connected sloping road in the loaded `bridge test - connect repro`
city. Check the joins to the two original roads and any terrain/structure changes
along the middle. Try the same endpoints with profile off/on after reloading the
baseline. Report obvious preview/Apply or surface problems; a small cosmetic kink
does not invalidate the measured curve agreement. Actual vehicle traversal remains
a separate manual check.

## Reuse of this finding

Future Connect elevation/terrain work must distinguish authored curve, split-course
height resampling, structural classification, and rendered surface finishing.
Slope edits do not use this new-course adapter: do not apply this fix globally to
Slope or Curve. Their future joint tests should nevertheless capture these distinct
representations instead of assuming matching node heights imply matching surfaces.
