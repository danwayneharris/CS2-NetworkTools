# 2026-10-01 — Rail/highway Apply blocked after Slope integration

Saved the preceding plugin recommendation verbatim in deferred-debugging-plugins.md.
Plugin setup is deferred at the user's request. Inspect the current selected rail
preview read-only; do not clear selection or Apply while investigating. The user
reports the same symptom on the main highway selection.

## Read-only findings

Live citySession 8ae4819cabe148d79e6ee7e5192acb01; paused. CurveSmooth strength 1,
revision 381/submission 236, six edges/seven nodes, interior junction 52182:3.
All selected curves match their expected preview control points; original inputs
match. Apply rejection is in InteriorJunction validation, not failed curve fitting.

The bridge resolves the actual connected temporary junction to 51649:37. It has
four incident preview entities for three original incident edges:

- 51717:151 -> original 334268:3 (Parent)
- 51719:43 -> original 334283:3 (Parent)
- 51723:167 -> original 334267:3 (Delete, Hidden)
- 51640:45 -> original 334266:3 (Combine), the next edge beyond the unselected branch

The two branch-related track connections use owner 51640, not 51723. The current
ReadInteriorConnections owner map covers only the three original incident edges,
so this returns unavailable and eventually rejects as "missing or ambiguous native
connections". This is evidence of native preview combining the unselected branch
with its neighbor; not proof of a visually broken rail connection.

Prior highway selections in the same log reject as "unselected curve changed",
even at strength zero. The highway preview is no longer selected, so identical
root cause is not yet established. Need capture it separately.

Proposed next experiment: prevent unintended native simplification/combining in
RoadShape preview definitions while retaining actual endpoint associations, then
verify original-to-preview topology, all directed connections and Apply agreement.
Do not merely ignore deleted/combined entities or relax the gate. This turn made
no gameplay/source behavior changes and did not clear or Apply the selection.

Screen capture through Computer Use was unavailable (native pipe not found).
Findings are from live bridge data and Player.log, not a rendered-frame inspection.

## Scope clarification: neighboring geometry

Dan permits considering slight outside-selection adjustments as future behavior,
with an explicit user-facing allow/disallow/tolerance policy. Recorded in the
feature plan; deferred from this preview-representation bugfix. Existing geometry
checks remain in place for now. No code or game state changed.
