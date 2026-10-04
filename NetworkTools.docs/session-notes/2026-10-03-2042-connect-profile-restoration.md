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
