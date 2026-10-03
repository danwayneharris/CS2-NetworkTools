# Finishing input preparation prototype — 2026-10-03 01:00 Pacific

Previous increment 459e736 proved the one-pass managed correction does not survive
an ordinary reload. Dan's visual confirmation still applies to the bounded result.
No persistent runtime change has been enabled; the broader native-rebuild scope
question remains pending. Useful offline work can continue independently.

## Smaller correction hypothesis

Installed source: `Game.Net/GeometrySystem.cs`, `FinishEdgeGeometryJob.Execute`,
lines 1721–1740 (verified source manifest Game 1.6.2f1). The two map lookups replace
four Y coordinates apiece before slope limiting, length calculation, bounds and
terrain sampling. Prefilling those same Y coordinates with the actual computed map
values makes a missed lookup harmless. A successful native lookup writes identical
values again. This avoids replacing the finishing algorithm or repairing after
limiting has already occurred. No authored Curve or terrain-height fudge is involved.

Implemented ONLY in the offline NativeReplay harness:
- `FinishHeightPreparation` assigns the eight named destination fields.
- `--pipeline-prepared` runs the original source pipeline with the instruction-derived
  native lookup model, prefilling present computed height-map entries before Finish.
- Source-derived flatten output supplies the values. No captured intermediate is
  substituted. Original reports and native observations are unchanged.
- `test-finish-height-preparation.py` runs native, managed and prepared modes, checks
  exact equality of computed EdgeGeometry and Start/EndNodeGeometry, and requires a
  discriminating case where corrected calculation differs from the old observation.

## Evidence

Standalone .NET 8 harness build: 0 warnings/errors, no deployment.
All five immutable cohort cases reproduce their original native observations with
preparation disabled. Prepared outputs exactly equal managed outputs for all five:
8, 5, 7, 8 and 5 edges; 7, 4, 7, 7 and 4 endpoint entries prepared respectively.
Four cases intentionally disagree with old native observations; that is retained in
reports, not labeled a passing native differential. Anchor preview remains consistent.
All 18 existing pipeline execution/negative contracts pass after the change.
Artifacts: `finish-preparation-cohort-01`, `finish-preparation-contracts-01`.
Compact cohort result retained next to this note.

The script's final discriminating-case requirement was added after the five-case run;
verified against those existing reports without repeating the expensive replay. It
requires at least one prepared endpoint and both corrected modes disagreeing with
old observed geometry. This is not live verification of the prototype.

## Proposed runtime design — not implemented or enabled

A Debug-only, version-guarded scheduling wrapper could schedule a small managed
preparation job after the original flatten dependency and pass its handle to the
unchanged Burst Finish scheduler. Use the same deferred entity list, read-only height
map and writable EdgeGeometry lookup. Do not read deferred list length on the main
thread or dispose native containers owned by GeometrySystem. Do not force a global
Burst-off setting. The original downstream job chain must include preparation.

This may avoid the synchronous barrier and full managed Finish loop used in the
bounded proof. It still needs compilation and live dependency/lifetime validation;
no performance advantage is claimed from source inspection alone. Cache any reflection
field accessors, validate callsite count/signature and assembly fingerprints before
patching, and ensure unsupported versions do not partially install. Existing research
trace transpilers may target the same scheduler; patch order must be tested or
explicitly reject overlapping instrumentation rather than silently bypassing either.

Persistent correction necessarily affects ordinary native rebuilds while installed,
including vanilla edits and loading. It should not be hidden inside the generic
Bridge transport nor depend on an active NT selection. Product placement and activation
remain a scope decision. If placed in NT, keep the small compatibility component
isolated from feature fitting and document its full effect/removal limitations.

Next qualification after scope approval: build/scheduler negative contracts; baseline
preview/Apply; reload of corrected checkpoint; ordinary connected edit/rebuild;
determinism/repetition; profiler timing on bounded larger toy updates; patch cleanup
and version rejection. Keep source-managed and native instruction models independent.
No vehicle traversal or human review of a persistent runtime candidate exists yet.
Game is untouched this increment and remains at the paused reloaded experiment save.
