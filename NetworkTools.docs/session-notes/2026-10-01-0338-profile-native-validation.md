# Constant Slope: first native validation and sequence runner

The Debug build from `2978a6b` completed compilation, postprocessing, UI build and
local deployment. DLL SHA256: `0A748E74D562860E7110C2FA028D19B2AC83D1F045D4EBB5B737C0015940E90B`.
Release compatibility is not established by this run.

On a fresh, hash-verified terrain v1.1 baseline, Curve strength 1 followed by
Constant Slope (both boundary smoothing options off) passed independent native
preview/permanent checks. Topology, directed connections and physical lane mapping
were preserved; selected and incident native preview curves matched Apply exactly.
Horizontal node positions and outer endpoints were unchanged by Slope. The one
unselected incident branch followed the moved junction vertically, within 0.034 mm
of the expected endpoint/handle translations; this is not an unchanged-outside claim.

The independent Python numerical model predicts permanent controls within 0.053 mm.
Maximum sampled absolute grade on this seven-edge selection fell from the old
Constant Slope result's 2.6373% to 2.4355%. The largest endpoint grade disagreement
is 0.001155 percentage points (float-scale); no tolerances were relaxed.
Reversing selection reproduced the same full-region permanent geometry fingerprint,
`27ea3e5b1fff723acf513680a991f091571cf245b1c119a83ef42756334cf2aa`.
This seven-edge fixture includes two main-highway edges; it is not the archived
five-edge ramp-only counterexample and must not be described as that exact repro.

Added a checkpointed, bounded sequence runner using existing lifecycle and validation
helpers, and an offline profile/generated-boundary summary. The runner starts each
independent sequence from the verified baseline; Slope-to-Curve uses the measured
intermediate fingerprint, not a falsely claimed original-baseline fingerprint.
Native failures stop the sequence rather than retrying an uncertain Apply.

Raw evidence remains ignored under `artifacts/profile-curve-ramp`,
`profile-curve-then-linear`, and `profile-reverse-ramp`.
Confidence: numerical and native authored-geometry agreement established for this
case; generated/rendered surface appearance, terrain causation, traversal and human
visual acceptance remain unverified. Slope-first and other layouts are next.
