# October 4, 2026 — Why automated checks missed BUG-008

Bounded source review only; no game mutation, algorithm change, or new test run.

## Established learning: the oracle covers correctness, not visual quality

- `NetworkTools.ConnectProfile.Tests/Program.cs`, particularly `EndpointGrades` and the Simple/Complex fixtures, checks endpoint heights/grades, shared height/grade, expected analytic values, reversal, unchanged horizontal input and invalid-input handling. A continuous but visually undesirable climb can satisfy these contracts. The fixtures include curved and differing-height inputs, so describing this as simply "we only tested flat roads" would be incorrect.
- `scripts/exercise-tool-provider.py`, `connect_preview_error`, `connect_preservation` and `assert_connect_report`, checks one-to-one preview/permanent curve agreement, connectivity/prefab and preservation of existing geometry. Separate lane checks address directed connectivity. These are necessary independent integration checks, but faithful construction of an undesirable candidate can pass all of them. The runner explicitly excludes visual, rendered-surface and vehicle validation.
- `scripts/inspect-connect-profile.py` is explicitly diagnostic, not an acceptance oracle. Its no-gap and matching-grade results for this capture rule out that particular authored/native seam; they do not rate the distribution of the climb or rendered appearance. The earlier verbal assessment inferred too much from these measurements.
- This exact edited case was not subjected to a controlled before/after automated Apply comparison during this inspection. Do not claim a demonstrated full-suite false pass on it. What is established is a missing quality criterion plus an overconfident interpretation of the available preview evidence.

This is an **oracle gap**: the tests answer "did we preserve the contracts and realize the candidate?", not "was that candidate a visually good solution?" There is also a separate surface-observation gap: authored cubics alone do not certify terrain or road-surface generation. The existing 5 cm tolerance is unrelated; tightening it would not catch an unattractive curve that is reproduced exactly.

## Deferred, bounded next investigation

Preserve original failing inputs and the MoveIt reference `bridge test - connect repro 2 - looks better` separately. Before adding a quality assertion, compare both authored profiles and generated surfaces at corresponding horizontal stations. Report grade versus station, change of grade per distance, and where the climb concentrates; compare screenshots at a shared camera. These are diagnostic candidates, not yet validated perceptual metrics or automatic rejection thresholds. A maximum grade alone would not establish visual quality.

If these measurements separate the two results, retain a compact fixture and a discriminating regression with an explicit contract. If only surfaces/terrain differ, investigate that generation stage rather than changing the fitter on speculation. Recovering exact MoveIt edits, deriving a general quality objective and calibrating thresholds are deferred; they exceed this low-priority documentation task. Do not add a self-confirming test that merely reproduces the existing formula, weaken connectivity checks, or turn a visual-quality warning into a new Apply blocker without an agreed policy.

BUG-008 remains non-blocking by Dan's decision. No new broad visual or vehicle qualification is claimed.
