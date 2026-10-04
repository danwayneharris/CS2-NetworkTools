# Connect course-height investigation — October 3, 2026

Status: investigation in progress; no runtime correction deployed. PR #20 remains
draft. Dan authorized fixing the rejected sloped connection, preferring shared
endpoint height first and smooth grades where practical. Neighbor edits should
require an explicit opt-in if they prove necessary.

## Captured evidence

Build `345533723c89`, clean Debug, rejects the selected Simple Connect with
`profile_native_CurveMismatch_0` when Smooth elevation profile is enabled.
Dan confirmed disabling it allows Apply. This is distinct from BUG-001.
Raw read-only response: `artifacts/pr20-connect-rejection-20261003-201202/002-invoke_provider.json`.
No Apply, selection edits, deployment or game restart were performed during capture.

`scripts/inspect-connect-profile.py` analyzes the capture without game access.
It matches native endpoint chord projections to authored cubic intervals and
reports controls and grades. This diagnostic assumes monotone Simple Curve;
it is not a replacement acceptance oracle or a rendered-surface measurement.

- Both outer endpoint height errors: exactly zero in the capture.
- All three internal native edge joins: exactly zero height gap.
- Horizontal control residual: at most 0.000304 m, negligible under agreed policy.
- Vertical control residual: up to 3.390506 m. This is a control-point discrepancy,
  not a claim that the rendered surface is displaced by that amount.
- Requested start/end grades: +0.2200% / +0.1396%, oriented start to end.
- Native start/end grades: -1.5610% / -12.1597%.

The shared-height requirement is already met in authored edge geometry. The
rejection has a substantial vertical-profile basis, not a millimeter-only mismatch.
Rendered junction surfaces have not yet been independently measured in this case.

## Game mechanics and prior knowledge

Installed source baseline: CS2 1.6.2f1; source manifest and setup record consulted.
Local source root: `cs2-decompile/src/Game` (proprietary source stays outside NT).

`Game.Tools/CourseSplitSystem.cs`:

- `CourseHeightData` constructor, around lines 1340–1485, samples terrain/water,
  derives height constraints and limits slope. For terrain-sampled courses it does
  not simply preserve the input cubic's interior Y profile.
- `SampleCourseHeight`, lines 1679–1780, cuts the horizontal curve, retains endpoint
  heights when FreeHeight is absent, then reconstructs vertical handles from sampled
  heights at one-third/two-thirds of the course interval.
- `SampleHeight`, lines 1783 onward, interpolates the sampled height buffer.
- `AddCourse`, lines 3781 onward, emits split definitions and increments random seed
  by course index. Arbitrary custom components are not automatically copied onto
  every split definition. A future scoped adapter must establish ownership rather
  than assuming a marker survives splitting or using random seed as unique identity.

This reinforces the course-sampling limitation already documented in
`connect-elevation-profile.md`; it is not a newly discovered general terrain system.
The additional evidence is the concrete zero-gap/large-grade-change counterexample.
Do not conflate this creation-stage operation with the previous road-surface
cutting/height-limiting or finishing experiments.

## Integration and next work

1. Keep the existing authored/native acceptance guard. Do not label the legacy
   terrain-generated curve smooth merely to enable Apply.
2. Investigate a narrowly scoped creation-stage correction for the new connection,
   retaining native splitting/intersections and explicit endpoint identity. Both
   preview and Apply must consume the same corrected candidate.
3. Before any interception, establish split-definition ownership, barriers/job
   completion, and structural elevation consistency; correcting course Y after
   classification could otherwise leave bridge/tunnel metadata inconsistent.
4. Turn this compact capture into a regression for the chosen production adapter.
   The existing offline native harness covers later geometry stages; it does not
   automatically establish CourseSplit behavior. Add a bounded stage harness if
   needed rather than claiming existing coverage extends here.
5. If preserving the existing incident grade cannot work, propose bounded,
   player-authorized neighbor adjustments. Do not silently implement them.

These findings belong in NT's Connect guide and future terrain-aware/Smart Connect
plans. Generic bridge capture improvements, if needed, remain generic and separate.
