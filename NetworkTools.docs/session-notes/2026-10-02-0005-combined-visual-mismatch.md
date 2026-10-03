# Remaining combined preview visual mismatch

Dan reports a large visible preview/Apply difference after the structural metadata
fix. Do not generalize the prior one-edge independent Slope result to combined
mode or claim full rendered fidelity from edge control-point agreement.

Captured the user's new selection in citySession
10687c914f914bd294deb3ccf5b09bef: Combined CurveSmooth, strength 1, both smooth
boundary options on, two edges / three nodes including an interior junction.
Submission 184. User had reloaded/changed the toy state since the previous test.

First capture stopped before Apply: the bridge's shared-endpoint discovery reports
unsupported for a degree-one end node. Extended the comparison tool to preserve
that explicit status and use the single uniquely mapped edge only for edge-geometry
coverage. This is not a synthesized successful junction/lane observation. All
interior path nodes are now captured; incomplete selected-edge coverage stops.

Checkpoint before the single Apply:
CitiesIIAgentBridge-surface-preview-before-apply-20261002-070428-c983f9ef.
Captures: ignored artifacts/combined-mismatch-native2, earlier aborted capture
artifacts/combined-mismatch-native, read-only discovery combined-mismatch-selected.

Results after Apply:
- All five observed authored curves match preview exactly.
- All five edge and end composition summaries match.
- Four EdgeGeometry surfaces match exactly; one incident edge has 0.335mm maximum
  control-point difference, acceptable under the 5cm policy.
- Directed car/track transition sets unchanged at all three permanent path nodes.
- Sampled terrain heights unchanged in this test (225 samples).

These checks do NOT explain or disprove Dan's visual observation. Native
StartNodeGeometry/EndNodeGeometry, end caps, rendered meshes and wider terrain
remain outside the recorded edge-surface oracle. Requested the precise visible
location to target the next capture. No further runtime fix attempted. Before/after
screenshots are in conversation; camera was unchanged. Paused throughout.

Diagnostic scripts changed only; deployed build remains 04792dd, SHA256
48EF3D8D36626D73C61F80133850509F6E8688337F05CF185533225636D0AEE9.

Final result checkpoint verified: CitiesIIAgentBridge-combined-visual-mismatch-after-20261002-070743-2b3bace9. Game paused; selection cleared by Apply. No further edits.
