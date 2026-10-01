# Labeled terrain v1.1 inspection: correct area

The labeled screenshot exposed an error in the previous inspection: queries were
centered on the old flat toy area, not the newly constructed terrain examples.
The previous elevation figures describe those old networks. They do not establish
coverage gaps in the user's new first batch. This was explained to the user.

Read-only get_camera located the new area near (-3262,-1777). A radius-2000 edge
query plus overlapping node queries captured all endpoints: 44 edges, 48 nodes,
four distinct components matching the screenshot. No game state was mutated.
Paused city session remained d3f30bfb3d3f4261b0d4e33741acde98.

Labels mapped to terrain-v11-analysis component numbers:
- Highway ramps (1): 14 edges, 27.24 m node elevation range; both junction snapshots
  complete. Mainline prefabs are TWO-WAY 2/3-lane highways with one-way 1-lane ramps.
  Nine directed lane transitions per junction include same-owner returns. Do not
  mistake this for the previous one-way highway lane-math fixture.
- Road hill jank (2): 9 edges, 27.61 m range. Ordered heights climb from ~602 to
  ~630 m with intermediate local bumps; suitable sustained-climb case.
- Rail merge (3): 9 edges, 20.44 m range. Four directed rail transitions join
  both inputs with the common arm, suitable working-merge preservation baseline.
- Peak and valley (4): 12 edges, 29.22 m range. Ordered profile begins ~629.7 m,
  crests at 645.1 m, dips to 615.8 m, and returns to ~629.7 m.

Terrain samples at nodes vary comparably, confirming actual non-flat terrain.
Sampled authored cubic peak grades are ~24.5% climb road, ~20.9% rolling road,
~47.1% rail and ~31.1% highway. These are local tangent estimates, not average
slopes or a verdict on game traversability. Retain them as existing stress inputs.

All four requested first-batch categories are present. No further construction is
needed before beginning regression work. Vehicle traversal, intended highway lane
semantics, smoothing and permanent Apply remain untested on this baseline.

The user-facing save name is bridge test - terrain and elevation v1.1; its actual
local filename percent-encodes the dot as v1%2E1.cok. The failed exact-name file
lookup was explained by this encoding, not a missing save. Both v1 and v1.1 exist.
