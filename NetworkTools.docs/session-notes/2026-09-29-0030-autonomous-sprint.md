# 2026-09-29 0030 - Autonomous regression and split-point sprint

Verified merged main checkpoints: NT 2180aec, bridge f0d6882. New local dan/autonomous-regressions branches start from these. Existing NT UI package-lock change remains user-owned and unstaged. No publishing authorized.

Priorities: reusable live regression runner; fresh baseline discovery and independent Apply verification; representative toy networks; tangent-continuous pinned split nodes; bounded longer-route and slope architecture investigations. Preserve baseline saves, use unique checkpoints, stay paused, no force killing. Record failures as well as successes and separate offline/native/permanent/visual confidence.

## Runner increment and first live blocker

Implemented scripts/live-regression.py: explicit mutation opt-in, fresh geometry-based endpoint discovery, identity-independent baseline fingerprint, fixed city-session check, request/response capture, no mutation replay, bounded polling, verified unique checkpoint package, strength sweep and independent permanent topology/elevation/connection/incident-curve comparisons. Four offline guard tests pass. Fixture has rail, four-way road branch, highway on/off ramp cases.

First discovery failed because get_network_edges includes long edges with far nodes outside get_network radius. Fingerprint now explicitly marks absent far-node positions; scope remains bounded, not whole-map validation. Discovery found 37 nodes and 39 edges. First live attempt saved a valid checkpoint then stopped at nt_activate: Debug adapter unavailable. No smoothing occurred. Added earlier adapter preflight and baseline save hash guard.

Environment evidence: local mod folder is .NetworkTools (disabled); content_load.json lists the large normal playset. User-specified reduced test playset is not currently active. Gracefully closed PID 49932 after preserving checkpoint; no forced termination. Asked for reduced playset name while continuing offline. Current runner remains a prototype: broader polling stability, complete selected-path preview comparison and forbidden crossing fixtures still pending. Existing connection comparison uses directed lane endpoint identities, not counts.
