# Saved pre-Apply comparison baseline

User recreated the failing network with the restored pre-search deployment and
reports saving before smoothing. Read-only bridge captures saved under
`captures/comparison-save-20260928`; no Apply or game mutation performed.

Latest trace submission20, revision54, strength1, selection50997:283 ->51001:3,
three selected edges. Existing freshness probe reports matching revisions,
original inputs and all three expected curves, with no ambiguity or mismatches.

Permanent junction51001:3 has four directed track connectors. Connected preview
resolves uniquely to327174:25 and has three. Both snapshots complete, no errors.
City session: ffd1b4292e8e4e04a7849a4c422cfec7.

Missing normalized direction: **51009 lane1 ->328369 lane2**.
Remaining:51006:1026 ->51009:2;51009:1 ->51006:1025;328369:1 ->51009:2.
Thus the saved case reproduces the original 4-to-3 connector loss before Apply.
The exact save name has not been queried; user owns the saved baseline.

Next: close game, deploy experimental search, reload this unchanged save and
repeat the same selection at strength1. Capture before Apply and compare all four
directed connections. This observation confirms the regression fixture, not the fix.
