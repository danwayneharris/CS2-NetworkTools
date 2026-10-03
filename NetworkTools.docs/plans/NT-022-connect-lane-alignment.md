# NT-022: explicit lateral lane alignment

Status: reviewed planning draft; saved October 3, 2026. Not yet executing.
Order: 6 of 6. Dependency: [NT-003](NT-003-connect-lane-direction.md).
Proposed branch: dan/nt-022-connect-lane-alignment.
All [shared execution rules](first-six-execution-contract.md) and [documentation requirements](README.md#required-implementation-document-all-plans) apply.

## Intended behavior and UX

Start with Simple/Complex Connect and ordinary road vehicle lanes. Extend the
endpoint lane diagrams with **Align selected lanes**. The player chooses equal-size,
contiguous, same-direction groups at the two approaches. Display their ordered
correspondence and which side has extra lanes. Never silently choose the lane pair.

Default remains existing centerline behavior. Explain unsupported mixed/shared-lane
mappings. Keep this experimental feature Debug-only. Do not extend alignment to
Curve/Slope or relocate existing junction nodes during this stage.

## Implementation

1. Reuse NT-003's lane identities, approach context, and invalidation rules.
2. Compute lateral offsets from actual lane positions/widths in consistently
   oriented endpoint frames. Use the chosen new-road prefab's lane layout.
3. Represent correspondence in the candidate and diagnostics. Keep lateral
   constraints separate from tangent and vertical-profile constraints.
4. Adjust only the new connection's permissible endpoint/transition geometry,
   keeping existing nodes and topology fixed. Provide a smooth lateral transition.
5. Validate each selected lane's resulting position and directed connection.
   Unequal spacing or native reconstruction can make exact alignment impossible.
   Reject/report incompatible correspondence rather than silently choosing another.
6. Do not move neighboring networks to satisfy alignment. Expose matching
   provider inputs and status through NT's existing generic provider.
7. Perform the focused architecture follow-up promised by NT-023: assess actual
   reuse and conflict hotspots introduced by NT-002/003/022; defer broad cleanup.

Native lane generation remains the independent oracle. A mathematically aligned
prefab centerline is insufficient evidence of resulting lane alignment.

## Verification and acceptance

Cover 2→3 and 3→2 one-way transitions; either side gaining a lane; differing widths;
curved approaches; reversed selection; two-way carriageways; incompatible groups;
stale composition; and smooth elevation profile interaction.

Inspect individual lane correspondence, preview/permanent results, unselected
geometry, and reload. Preserve intended connections and detect forbidden ones.
Apply the shared 5 cm positional policy without relaxing lane identity/topology.

When the native geometry cannot realize a chosen correspondence within the supported
scope, demonstrate an explicit unsupported/rejected result. Do not silently add a
neighbor-editing or direct lane-routing feature.

## Documentation and Dan's review

Create a durable lane-alignment guide with features/usage, architecture, actual tests,
limitations, and **Dan's review**. Also finish the six-stage review index.

1. In a 2→3-lane fixture, select the left pair, then the right pair.
2. Inspect which pair continues and which side gains the extra lane.
3. Compare preview with Apply and check markings/taper quality.
4. Follow the supplied vehicle-traversal check separately from geometry review.

Expected: explicit correspondence visibly controls alignment, unsupported mappings
are clear, and no neighboring road moves silently. Human visual/traffic review
remains pending until Dan performs it.

## Delivery

One draft PR stacked on NT-003; alignment fixtures/evidence; focused architecture
follow-up; and the consolidated six-stage review guide with exact build/save identities.
Do not merge any PR in this autonomous stage.
