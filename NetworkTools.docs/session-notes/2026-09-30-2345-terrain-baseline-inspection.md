# Read-only terrain toy baseline inspection

User prepared `bridge test - terrain and elevation v1`. Located that save package
and inspected paused live Wantagh, population zero, city session
d3f30bfb3d3f4261b0d4e33741acde98. Save name alone is not a geometry identity;
these captures establish the observed live geometry for future fixture preparation.
No selection, preview, Apply, save, simulation or lifecycle mutation was performed.

New branch dan/terrain-regressions starts at merged main 4b5fc1c. Original user
worktree and bridge remain untouched. Reusable offline analysis is in
scripts/inspect-terrain-capture.py; captures and component-numbered map are under
session-notes/captures/terrain-inspection-*.

Two overlapping edge queries and node queries cover all endpoints of the 50
captured edges (56 nodes). This is regional coverage of the toy area, not a
whole-map absence claim. Terrain was sampled at the captured node positions.

Component numbers refer to network-map.png:
- 1: old rail control, node elevations flat.
- 2: highway ramps, node elevation spread 0.198 m, terrain-at-node spread 0.373 m.
- 3: four-way road control, node elevations flat.
- 4: six-edge isolated road, node elevation spread 1.378 m; useful mild profile.
- 5: rail merge nearly flat (0.063 m spread); only two directed transitions between
  two of the three incident owners. Treat third branch as already disconnected,
  not a working-merge preservation baseline.
- 6: eight-edge rail merge, node elevation spread 5.691 m, terrain spread 5.741 m.
  Four directed transitions connect both branches to the common arm.

Both highway junction snapshots contain five directed transitions. These are
baseline observations, not proof of intended lane choice or vehicle traversal.
Sampled authored cubic tangent grades reach about 6.3% on road 4, 19% on highway
2, and 26.8% on rail 6. These are local sampled peaks, not average grades or game
legality verdicts; small net elevation range can coexist with sharp handle-induced
vertical changes. Preserve these as stress cases rather than hiding the evidence.

Conclusion: useful starting coverage, especially elevated rail, but the observed
road/highway layouts do not yet provide substantial sustained climbs and a separate
broad crest/dip road case. Recommend adding those without replacing these mild and
stress examples. No smoothing correctness has been tested in this inspection.
