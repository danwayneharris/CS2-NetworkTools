# Non-blocking bug backlog

This list records known issues accepted for continued experimental review. It is not a release-readiness claim. Feature priorities remain in [ROADMAP.md](ROADMAP.md).

## BUG-001 — Connect initially disabled after a fresh game launch

- Priority: minor / non-blocking for PR #19, per Dan's review on October 3, 2026.
- Build observed: `1.5.7+g7dfb76ffeb88.clean.Debug`.
- Repro save: `bridge test - connect repro` (user-created; not yet checksummed or registered as an automated fixture).
- Reproduction reported by Dan: restart CS2, load this save, select the two endpoints in Simple Connect. The first preview appears but Create Connection is disabled. Right-click once to deselect only the ending node, then reselect it: the button enables and the connection works. Reloading the save within the same game process does not reproduce the first-attempt failure.
- Workaround: deselect and reselect the ending node.
- Evidence: Dan reported the cold-start/reload distinction. Agent inspection after reselection found preview accepted, CanApply and GetAllowApply true, UI binding Enabled, and a green button. The original disabled state was not captured; cause and introduction revision are unknown. No code change or restart caused that inspected recovery.
- Follow-up: capture the first disabled state after a fresh process launch, including candidate/input/submission identities, producer and observer readiness, native errors, and UI binding. Compare against the same selection after ending-node reselection. Do not bypass validation to hide the symptom.
- Broader complex Connect, vehicle traversal, and cold-start regression testing remain pending. Dan confirmed basic working-case behavior appears correct; this is not exhaustive qualification.

## BUG-002 — Complex Connect preview can be obscured by terrain

- Priority: low / non-blocking for PR #20, accepted by Dan on October 3, 2026.
- Reviewed fix build: `1.5.7+g02b899b89f9c.clean.Debug`.
- Observation: with Complex Curve, surrounding terrain can obscure portions of the preview road. Dan confirmed that the obscured road is correctly preserved after Apply and the game adjusts the terrain.
- Scope: a visual preview limitation; the exact terrain/rendering cause is not yet established. Do not infer missing road geometry from occlusion alone or bypass geometry/connectivity checks.
- Follow-up: compare preview and permanent terrain/surface generation on a compact reproduction. Low priority; does not block the accepted Connect profile fix. Visual acceptance does not establish vehicle traversal or broad terrain/prefab coverage.
