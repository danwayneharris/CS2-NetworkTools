# Interior rail preview at strength 1 — 2026-09-30

Maintainer observed a missing-looking lane and alternating preview shapes with an
interior rail junction. Held example development and captured read-only state;
no Apply, slider change, selection change, pause change or save operation performed.

Selected path has five nodes/four edges; interior junction 351045:7. Ten snapshots
sampled at strength 1 all show submission 608 and previewReady=true. Normalize temp
edge identities through owners.temp.original: all four permanent directed track
transitions remain present, with none added, in all ten snapshots. See retained
connection-comparison.json. No conclusion about rendered rail continuity or actual
vehicle traversal follows from graph reachability alone.

Player log shows alternating rotations through +12 degrees, then acceptance at
attempt 23/submission 608. This explains the earlier shape alternation as bounded
search; it was no longer alternating during capture. The bridge observation's
validationReady=false is intentionally hardcoded (no tool revision/rebuild guarantee),
not a failed native validation signal. Tool readiness is separately reported by NT.

Earlier interior tests used 0.5/0.8 and do not cover this full-strength case or this
selected input branch. Need a visual capture if the settled solution still has a
gap, and a dedicated strength-1 fixture/preview-vs-Apply test before ruling out a bug.
Do not silently treat this observation as a pass for visual correctness. UX should
make searching versus verified candidates clearer. Investigation remains open.
