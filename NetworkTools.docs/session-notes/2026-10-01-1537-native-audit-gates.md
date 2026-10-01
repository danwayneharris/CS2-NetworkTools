# Native audit gates for PR14

Dan authorized the three remaining automated gates: two-ended incident edit, stale-input rejection, and native Release supported-envelope checks, followed by restoring Debug. Work remains in isolated nt-audit-sprint on dan/audit-correctness-sprint. Initial status clean; game is closed. Preserve toy baseline packages and checkpoint created fixtures before mutations. No vehicle/visual approval is implied.

First fixture attempt: native Medium Road curved alternate between two internal hill-road nodes completed, but created two edges. The explicit single-edge requirement rejected the fixture; no Slope mutation occurred. Captures: artifacts/native-two-ended-create. Retain the failed experiment, reload before a straight-branch attempt.

Straight placement created one alternate edge. The selected route initially used it: the prefab penalty is additive, not a multiplier, and was insufficient to prefer the original path. Bowing that single edge's two inner controls by 40 m in Z lengthened it without changing topology. Native selection then matched all nine original edge identities; saved a separate baseline.

Two-ended Slope Apply passed: endpoint Y deltas 3.861206 m and 0.247 m; composed translation error 0.00001 m; exact preview/permanent agreement; topology, directed connections and physical lane mappings preserved. The additional existing audit initially lacked its expected changedUnselectedEdges report field; this harness now emits it and the captured report was supplemented from the retained before/after data, without replaying Apply.

Atomic native stale-input test passed: CanApply True -> False after a 1 m control-point edit; TryRequestApply returned False; restoring the original yielded True and phase remained Ready. Exact recipes and raw result are retained. No production mod changes so far. Release native gate next.
