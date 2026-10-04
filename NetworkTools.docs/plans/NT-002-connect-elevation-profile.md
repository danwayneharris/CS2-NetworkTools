# NT-002: optional smooth elevation profiles in Connect

Status: experimental implementation and native qualification on October 3, 2026. See [implemented scope and unresolved native course sampling](../connect-elevation-profile.md).
Order: 4 of 6. Dependency: [NT-023](NT-023-architecture-review.md).
Proposed branch: dan/nt-002-connect-elevation-profile.
All [shared execution rules](first-six-execution-contract.md) and [documentation requirements](README.md#required-implementation-document-all-plans) apply.

## Settled UX and scope

Add optional **Smooth elevation profile** for Simple Curve and Complex Curve.
Existing behavior remains the default. Loop stays unchanged and explains why the
new control is unavailable. Keep the new experimental option Debug-only.

The option smoothly joins endpoint heights and approach grades. It does not mean
constant slope, terrain-following, obstacle avoidance, or automatic bridge/tunnel
routing. Preserve the horizontal curve/handle workflow.

## Implementation

1. Establish explicit endpoint context: position, incident approach edge, traversal
   orientation, grade, prefab, and native endpoint/surface offsets. Require an
   approach-edge choice when multiple incident edges supply plausible context;
   do not silently take the first buffer entry.
2. Fit a coherent vertical profile over horizontal stationing, matching endpoint
   heights and chosen approach grades. Preserve continuity through Complex Curve's
   internal join. Use appropriate pure math from existing work while preserving
   Connect's creation semantics.
3. Keep authored geometry, generated native surfaces, and surrounding terrain
   observations distinct. Use terrain/native observations for diagnosis and
   acceptance; do not silently treat network-deformed terrain as desired geometry.
4. Generate one accepted candidate used by both manual and provider Apply.
   Endpoint/context/parameter changes invalidate previous evidence.
5. Preserve existing networks outside the declared native effects of the new
   connection. Record and inspect those effects explicitly, rather than accepting
   all native rebuilding changes as harmless.
6. Reject unsupported or invalid profiles with clear status. Do not silently
   drop the requested smooth-profile constraint to enable Apply.

No automatic extension of the global finishing compatibility experiment is implied.
Keep its activation and hash constraints explicit in fixtures and review builds.

## Interfaces

Add an NT parameter for the profile option and endpoint-context selection through
existing generated UI/provider mechanisms. Avoid a Bridge-specific command.
Diagnostics expose original context, candidate profile, native results, and reasons
for rejection. Keep legacy Connect defaults and Loop behavior unchanged.

## Verification and acceptance

Test flat/sloped endpoints, opposing grades, reversed selection, asymmetric spans,
Simple/Complex modes, changed horizontal handles, ambiguous approaches, and Loop's
unsupported control state.

Check one-to-one preview/permanent correspondence, intended directed connections,
existing geometry preservation, repeatability where meaningful, and reload. Include
a difficult ramp without promising perfectly constant rendered grade.

Use disposable toy copies with segments removed to obtain endpoints when needed.
Inspect the created fixture rather than trusting command acceptance.

## Documentation and Dan's review

Create/update a durable Connect profile guide: usage, architecture, actual tests,
limitations, and **Dan's review**. Record exact source/build/activation/save identities.

1. Generate the same connection with the option off and on.
2. Inspect endpoint joins and the Complex Curve midpoint.
3. Adjust horizontal handles and verify profile recomputation.
4. Compare preview, Apply, terrain around the join, and reload.

Expected: smoother grade joins with explicit constraints, not automatic terrain
routing or perfect grade on pathological geometry.

## Delivery

One draft PR stacked on NT-023, compact fixtures, profile diagnostics, and stage-
specific baseline/result checkpoints or a clear manual construction recipe.
