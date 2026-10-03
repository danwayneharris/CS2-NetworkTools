# Preview structural-composition investigation

Follow-up to 2026-10-01-2159-surface-preview-observations.md. Bounded diagnosis;
no runtime fix, deployment, or additional Apply. Recreated the same independent
SlopeEaseInOut preview on the checkpointed post-Apply toy state. Game remains paused.
Runtime 00c789a, Debug; DLL SHA256
6EC3B8AAF922CC332898ED89ADB1762CAAF0DB2BB91057AEE2E9885D0383F360.

## Concrete counterexample

Saved before/after capture already established identical authored curves but
mismatching generated surface controls. Composition comparison now reveals:
selected edge 358762:7 preview uses width 13, Elevated/Pavement; permanent uses
width 12, Pavement, ExclusiveGround/LowerToTerrain. This is structural metadata,
not a sub-5cm positional tolerance issue.

Read-only Unity component inspection of the recreated preview (submission 1485):

- Permanent selected edge 358762:7 has neither Elevation nor Upgraded.
- Both permanent endpoint nodes 74802:9 and 74801:9 lack Elevation.
- Temporary selected edge 72963:39 also lacks Elevation and Upgraded.
- Its temporary start node 51131:61 has Elevation (8,8). Temp original is null:
  this is a newly generated preview node, not a native copy of the original node.
- Temporary end node 51133:733 lacks Elevation; Temp original is also null.
- Adjacent original edge 79411:9 has Elevation (8,8) and Upgraded.Elevated;
  its start is the same original node 74802:9.

Entity versions are capture identities, not reusable fixture selectors. Current
preview request captures are in ignored artifacts/composition-probe. The compact
before/after summary now includes composition flags/state/width for all five
observed incident edges, not just curve/control-point errors.

## Source-grounded explanation

RoadShapeToolSystem.Jobs.cs:386-401 initially copies original node/edge elevation.
But lines 443-469 then clamp *both node elevations* based on each edge's structural
classification. Lines 474-480 also set ForceElevatedNode for both ends of any
Elevated edge. Thus the elevated neighbor's preview definition promotes the shared
node despite the permanent ground/elevated transition node lacking Elevation.
Apply writes geometry and keeps existing structural components instead.

Installed game 1.6.2f1 sources under the separate cs2-decompile repository:

- Game.Tools/GenerateNodesSystem.cs:1481-1502: an original-linked node inherits
  its original Elevation; a new node takes the requested elevation instead.
- Game.Tools/CourseSplitSystem.cs:3647-3664: force-elevated flags can promote
  endpoint elevations to the prefab threshold.
- Game.Prefabs/NetCompositionHelpers.cs:1710-1727: even one elevated endpoint
  can classify the edge as Elevated. A zero edge elevation does not prevent it.
- Game.Net/GeometrySystem.cs uses composition-dependent junction/surface rules.

This identifies a concrete preview/input-ownership defect. It does not prove
that correcting it removes all measured surface or terrain differences; terrain
changed after Apply and node rotations/cutting context may contribute separately.
No claim that combined mode introduced it: the counterexample is independent
EaseInOut and uses the shared RoadShape preview path.

## Focused next correction and qualification

Preserve edge and node structural metadata independently when editing existing
networks. In particular an elevated edge does not imply both nodes are elevated.
Avoid simply forcing every preview to ground or altering Apply to match the wrong
preview. Establish whether exact original node metadata can survive the native
preview generation while still supporting moved nodes and shared incident edges.

Qualify with this ground/elevated transition, a fully elevated span, tunnel portal,
ordinary ground road, and a moved junction with incident edges. Compare generated
composition and surface geometry in addition to authored curves and directed lanes.
Keep actual terrain deformation after Apply a separate observation.

## Verification / final state

Extended summarize-slope-surface-preview.py and replayed the saved capture offline;
it reports composition mismatches explicitly. No new game-code tests/build were
needed for this diagnostic-only change. Native readonly inspection above confirms
structural mismatch, but no proposed correction has been tested.

Permanent state remains saved as
CitiesIIAgentBridge-surface-preview-after-apply-20261002-045856-b9415c47.
Only a preview has been recreated since that checkpoint; no unsaved permanent
network changes. User package-lock.json modification remains excluded.
