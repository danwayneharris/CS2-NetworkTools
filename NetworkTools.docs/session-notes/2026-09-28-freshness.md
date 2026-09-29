# Preview freshness investigation

Following supervised 0.8 Apply verification, added a reusable capture fingerprint
and an offline acceptance-contract prototype. The saved 0.5/0.8 case demonstrates
same temporary entity identity with different geometry. Added regression coverage
for that evidence, collection failures, nonfinite geometry, and stale revision /
city / tool tokens, including returning to a previous slider value.

Source review confirms current CanApply waits for the math job, not a demonstrated
native lane reconstruction barrier. Documented the required revision lifecycle and
Apply-time recheck in ../preview-freshness.md. Native completion association remains
unresolved; no runtime gate or solver behavior changed, no deployment needed.

Verification: five Python tests passed against saved live evidence. This validates
the offline observation/contract checks only, not runtime stale-result protection.
Next: identify a native post-lane-rebuild observation hook and candidate revision
propagation before using native connectivity to authorize any newly supported case.

## Native ordering source investigation

Located an explicit candidate observer point after ModificationEndBarrier using
installed Game.dll, rather than relying on a delay or simulation frame. Confirmed
LaneSystem registers its producer with ModificationBarrier4 and the scheduler runs
later lane stages before ModificationEnd playback. See preview-freshness.md for
source locations, fingerprint, failed namespace probes and proposed registration.
No runtime observer is implemented yet. Next change is diagnostic instrumentation
with revision/candidate association, followed by a game restart and supervised
rapid-slider/cancel/reselect tests. Current game may stay open meanwhile.
