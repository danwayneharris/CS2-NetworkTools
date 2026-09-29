# External-input freshness prototype

Added original_fingerprint and observation_rejection to scripts/preview-freshness.py.
They accept only complete permanent schema-2 snapshots in the same city session,
check unique junction/incident owner coverage, and fingerprint captured owner/lane
inputs including endpoint versions, positions, curves and prefab/composition data.
Observation timestamps, Updated/Created and snapshot-local equality IDs are excluded.
Derived lane identities remain included conservatively: harmless rebuilds can cause
extra invalidation. Nested array ordering is not fully canonicalized.

Explicit rejection reasons distinguish stale session/revision, changed original
network, and unavailable snapshots. No detected invalidation is not a positive
native validation verdict. The prototype is offline only and does not establish
coverage beyond the bridge snapshot schema (for example uncaptured prefab fields).
It is not yet integrated into the runtime probe or CanApply.

Verification: five new tests pass plus five existing preview-freshness tests.
Synthetic mutations exercise node movement, handle movement, endpoint version and
prefab version changes with an unchanged tool revision. A real captured before/after
Apply pair also changes the fingerprint. Delayed revisions, city changes, incomplete
snapshots and duplicate owners reject. No game queries, edits or deployment needed.

Next implementation: capture equivalent bounded original inputs in NetworkTools at
submission and compare at observation/Apply, with world-scoped state and no bridge
runtime dependency. Keep current interior-junction restriction until constraint and
native-result validation have a tested integration.
