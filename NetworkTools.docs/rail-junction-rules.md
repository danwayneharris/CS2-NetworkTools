# Rail junction connection filter investigation

## Established from installed source

Game.dll SHA-256: AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A.
Targeted type extraction used ilspycmd 9.1.0.7988. Local extracted files are under
%TEMP%/nt-rail-rule-source; game source is not committed to this repository.
Line references below refer to that extraction; method names are the stable anchors.

- LaneSystem.cs:6567, CreateNodeTrackLanes: average source positions and tangents;
  group targets by owner and average theirs. Flatten tangent Y and normalize.
- LaneSystem.cs:6599–6615: use max(1 metre, 3D distance between connection positions)
  and compare NetUtils.CalculateCurviness with the SOURCE lane prefab's
  TrackLaneData.m_MaxCurviness. The accepted groups establish a target index range,
  so this is not simply an independent per-pair predicate for every possible layout.
- NetUtils.cs:305–309: for the supplied tangents, theta = acos(clamp(dot,-1,1));
  k = 2*sin(theta/2)/distance. For unit tangents this is the inverse radius of a
  circular arc with that turn angle and endpoint chord. It is not the maximum
  differential curvature of the eventual fitted cubic.
- LaneSystem.cs:6626–6642: additional non-intersection alignment filtering can skip
  nearly parallel tangents when the displacement does not point along them.
- LaneSystem.cs:6684, CanConnectTrack: another track eligibility path rejects U-turns,
  roundabouts, master lanes and incompatible track types, with a curviness check.
  The call at 6910 is for combined Road|Track lanes; do not conflate this with the
  pure-track grouping pass above.
- LaneSystem.cs:5210 and 5404–5441, GetNodeConnectPositions: actual connection points
  derive from EdgeGeometry side curves and composition lane offsets/widths. Stored
  centerline endpoints alone are not the complete inputs.
- Game.Prefabs.TrackLane.cs:21: component class default m_MaxCurviness is 1.8.
  NetInitializeSystem.cs:1551 converts the authored value with math.radians into
  TrackLaneData.m_MaxCurviness. The class default does NOT establish an individual
  loaded prefab's value. Nor is this a universal 1.8-degree maximum merge angle.
- LaneSystem.cs:7222–7224 clamps a generated TrackLane's recorded curviness to the
  prefab maximum. That output value is not an independent measurement of the
  candidate's rejection metric.

## Evidence from our saved pair

The script uses captured edge-lane terminal positions and planar tangents as a
proxy for generator ConnectPositions. Within this one unchanged city/edge identity
set, it matches PathNode owner/lane/curve/secondary labels; equalityId is only
meaningful inside an individual snapshot and is not compared across captures.

| Observed connector | Before angle / span / k | After angle / span / k | Outcome |
| --- | --- | --- | --- |
| Mainline to branch | 31.34 degrees / 22.17 m / 0.02436 | 47.64 degrees / 17.99 m / 0.04489 | Removed |
| Branch to mainline | 31.34 degrees / 28.36 m / 0.01905 | 47.64 degrees / 22.55 m / 0.03582 | Removed |
| Both mainline connectors | k below 0.000001 | k below 0.000003 | Retained |

The candidate class-default limit radians(1.8) = 0.031416 separates every observed
working/broken connector in this pair. This is a consistent hypothesis, NOT exact
reproduction of native eligibility. We have not captured the instance prefab limit,
composition lane groups or original generator ConnectPositions. No universal angle
constraint should be added to SmoothCurve from these numbers.

The meaningful geometric insight: this edit both increased the turn and reduced
available connection distance. Future fitting needs to consider both quantities.
Elevation also enters through the position distance even though directions are
flattened here; this check is not a grade/slope validation rule.

## Reproduce without the game

```powershell
uv run python scripts/test-rail-junction.py
uv run python scripts/analyze-rail-junction.py NetworkTools.docs/session-notes/captures/rail-merge-20260928/before.json NetworkTools.docs/session-notes/captures/rail-merge-20260928/after.json
```

Four tests cover an analytic circular case, distance clamp, planar tangent versus
3D distance, invalid tangent, cross-city rejection, and this captured pair's
consistency with the default-limit hypothesis. These are analysis tests, not game
simulation. Results are saved beside the captures as curviness-analysis.json.

## Next evidence needed

Extend the bridge snapshot with lane PrefabRef and actual TrackLaneData, plus
incident edge Composition, EdgeGeometry and composition lane definitions. Those
are required to reconstruct native group inputs rather than use terminal proxies.
Then re-capture the two saved states and evaluate the native gates before designing
the junction fitter. No game/system patching is indicated by this result.

Keep research local for now. Eventually fork the bridge into Dan's GitHub account
and push its development branch; do not open a junction PR yet.