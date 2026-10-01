# Terrain/profile experiment: visual review handoff

October 1, 2026. Experimental Debug build; not release qualification.

## Current surface-aware candidate

Constant Slope first performs the offset-aware vertical fit described below. Debug
builds additionally try a restricted native surface correction on supported ground
ramps. The captured off-ramp's junction raises its generated surface and shortens
the usable edge span; an authored constant-grade cubic alone does not produce a
constant-grade road surface there. The correction improves that profile without
moving the highway's authored geometry.

Dan visually confirmed that the first corrected candidate looked MUCH better and
the small discontinuity disappeared. Subsequent repeatability work preserved that
approximate objective and removed a fallback/feedback problem. Final preview settling
uses up to six candidates, stable native observations, and two consecutive candidate
changes within 5 cm. Apply uses the accepted reference. This is Debug-only; Release
retains the ordinary offset-aware Constant Slope fit.

Current review checkpoint:
`CitiesIIAgentBridge-review-surface-profile-settled-20261001-130556-32145a5a`.
Untouched source baseline: `bridge test - terrain and elevation v1.1`.
The separate first visually approved checkpoint is
`CitiesIIAgentBridge-review-surface-profile-first-20261001-122611-790afd05`.

The first native settling sequence used four candidates. Reversed repeat changed
controls by 1.155 mm and nodes by 0.143 mm; the next forward Apply left the entire
region geometry unchanged. Topology, directed/physical lane mappings, fixed endpoints,
authored XZ, unselected authored curves and authored preview/Apply agreement passed
for the off-ramp. These do not establish final mesh equivalence or vehicle traversal.
See the [repeatability record](session-notes/2026-10-01-0552-profile-repeatability.md)
for final-build checks and persistence results. On that final build, reversed Apply
after reload was exactly unchanged; another reload preserved geometry and normalized
directed/physical lane mappings at all 37 shared nodes. The game was left paused on
the settled checkpoint above, with no geometry changes after the final reload.
Full Debug deployment and Release C# compilation passed; full Release/Burst was not
run. Fresh-baseline hill-road, rail and highway-mainline smoke checks also passed.

Inspect the same five-edge ramp from junction X=-2573.077, Z=-1939.879 to dead end
X=-2392.361, Z=-2025.778, particularly its first bend and highway join. Confirm the
latest settled result retains the visual improvement. Perfect constant generated
grade is explicitly deferred. Terrain avoidance, bridges/tunnels, generalized ramp
support, traffic testing and full Release/Burst qualification remain open.

## Earlier offset-only checkpoint (historical)

The following evidence describes the earlier offset-only implementation, before
the additional surface-aware correction. Its exact idempotence and persistence
results must not be substituted for verification of the newer correction.

## What changed

Constant Slope now fits node heights and vertical Bezier controls together, accounting
for endpoint-to-node height offsets before fitting. It preserves the outer node
heights and horizontal geometry. Optional start/end smoothing retains the neighboring
endpoint grade. Curve smoothing, Ease and Arch behavior are unchanged.

On the captured five-edge off-ramp with identical horizontal controls, the old fit
reached an 8.023% grade magnitude near the first bend; the new fit reaches 7.220%
(nominal 7.153%). This supports an offset-fitting improvement, not a claim that all
terrain-related visual jank is fixed. A cubic's vertical profile is not exactly linear
in horizontal arc length everywhere.

See [investigation](terrain-profile-investigation.md) for source evidence and
[comparison plot](session-notes/plots/native-offset-profile-20261001.png).

## Evidence and limits

| Category | Result |
| --- | --- |
| Offline math | Geometry suite passes, including 605 vertical-profile assertions, invalid-input checks and native replay; independent replay error 0.0421 mm |
| Installed source | Network-adjusted terrain samples are distinct from pristine terrain; final rendered mesh behavior is not established by those samples |
| Native preview and Apply | Hill road, crest/dip road, rail merge, highway mainline, on/off ramps, ordinary pinned split, reverse selection and four boundary-smoothing combinations passed their recorded audits |
| Repeated Apply | Three Constant Slope applications on the full-strength ramp; second and third leave the region fingerprint exactly unchanged |
| Save/reload | Final checkpoint preserves exact region geometry and normalized directed/physical lane mappings at all 37 shared nodes |
| Human visual | Pending for this build |
| Vehicle traversal | Not tested |
| Release/Burst | Not qualified in this sprint |

Live geometric errors up to 0.05 m are accepted per Dan's instruction. The recorded
23.5183 mm rail-center displacement is acceptable, not a current failure. Topology,
intended/forbidden connections, lane semantics and stale-result protection remain
separate requirements. Historical raw failures retain the old threshold context.

Curve then Slope and Slope then Curve are not equivalent: Curve changes horizontal
lengths while preserving heights. The seven-edge reverse-selection result matches
its forward counterpart exactly. A coordinated offline refit prototype exists, but
no combined UI or new pin/elevation policy has been introduced.

The half-strength alignment changes both geometry and terrain placement; it is not
a controlled terrain-isolation experiment. Pristine terrain readback, terrain-only
variation, final mesh inspection and traffic traversal remain open. Slope is not a
terrain-following or grade-limited routing tool. Equal outer heights can produce a
flat fit that cuts terrain. Boundary smoothing can intentionally vary the grade.

## What to inspect

The game is left paused on the full-strength checkpoint below. No unsaved changes
were made after reloading it, and no debugger suspension is held. Other networks
remain unchanged in these named review saves.

- Full: `CitiesIIAgentBridge-review-profile-ramp-full-20261001-110730-95015e9d`
- Half: `CitiesIIAgentBridge-review-profile-ramp-half-20261001-110515-ad4d7973`
- Recovery before reload: `CitiesIIAgentBridge-regression-before-reload-20261001-110934-3c02e242`
- Untouched baseline: `bridge test - terrain and elevation v1.1`

These are local `.cok` saves in the normal Steam-user save directory. Both review
cases select only the five-edge off-ramp, from junction X=-2573.077, Z=-1939.879 to
its dead end X=-2392.361, Z=-2025.778. The earlier seven-edge fixture also includes
two mainline edges and therefore has different fixed endpoints.

1. Inspect the first curved ramp segment and its connection to the highway from
   a low side view: any abrupt vertical kink or undesirable grade bulge?
2. Inspect the steep hillside, embankment/retaining structures and road surface
   for clipping or terrain deformation that the centerline checks cannot establish.
3. Compare the half-strength checkpoint if useful. Both apply Curve first, then
   Constant Slope, with start/end boundary smoothing off. Full was additionally
   re-applied twice to verify idempotence.
4. Give a visual verdict before deciding on a combined curve/slope mode. Vehicle
   traversal can remain a separate later check.

## Build and local history

NetworkTools worktree: `nt-terrain-profile`, branch `dan/terrain-profile-sprint`,
based on merged main `ed3ab81`. Production change `2978a6b`; later commits add
verification, diagnostics and documentation. Debug build completed compilation,
postprocessing, UI build and local deployment. Deployed NetworkTools.dll SHA256:
`0A748E74D562860E7110C2FA028D19B2AC83D1F045D4EBB5B737C0015940E90B`.

Bridge worktree: `bridge-terrain-profile`, branch `dan/terrain-observation-sprint`,
based on `8843041`, head `9ffceb8`. Bridge changes are documentation only; no new
bridge implementation, schema or release dependency was needed.

Notable NT commits: `e6a7b29` pure fitter; `2978a6b` integration; `1f730b8`/`d42ae18` native replay fixture;
`d14177b` live 5 cm acceptance; `9c8b0d8` boundary/coordinated analysis;
`aa2f6e1` review preparation and persistence lane comparison.

Raw evidence remains in ignored `artifacts/`, including `profile-offramp-repeat`,
`profile-offramp-half`, `profile-final-before-reload` and
`profile-final-after-reload`. Reusable scripts, selected fixture and plots are tracked.
Nothing was pushed or published. Original worktree user changes were preserved.
