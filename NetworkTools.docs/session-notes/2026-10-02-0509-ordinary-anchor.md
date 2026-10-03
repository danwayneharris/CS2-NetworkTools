# Ordinary anchor observation — 2026-10-02 05:09 PDT

Continue from 3ac27d3. The previous turn made concrete progress: five-stage offline
execution, four native comparisons and ten validation cases.

Read historical junction-cut-apply/before.json and the current terrain preview.
All four central authored curves match exactly across these captures. Central
connected temporary node prefab remains the same 8 m asset (16071:1), with null
Temp original and Create/IsLast flags. Historical/current preview surfaces differ
only at small numeric scale in the inspected values; the outstanding difference
is the permanent result. Composition entity identities differ between sessions;
do not mistake identity churn for a semantic composition difference.

Fresh Bridge state: Wantagh population 0, paused, controls enabled/remembered,
process 44740 responsive. Trace disarmed and STOP absent. Requested unique
checkpoint adb4c9c0107f4d209e07f25c256badd7 before the next preview operation;
must verify completion and saved package before proceeding.

Add an ordinary mode to the existing capture driver, plus optional junction
observation bracketed by matching ready provider session/revision/submission.
This records correlation limits explicitly and rejects a missing preview instead
of counting it as evidence. No Apply added to this driver.

Checkpoint completed and its hash is retained under Bridge artifacts/schedule-live.
First invocation incorrectly supplied bridge.ps1 instead of the Bridge root; failed
before sending a command (ordinary-combined-01). Corrected ordinary-combined-02
captured a complete connected preview between matching ready provider tokens.
Then compare-current-slope-preview.py checkpointed and applied submission 14 once.
ordinary-apply-01 retains all before/after snapshots and 1548 terrain samples.
All five authored curves, generated edge surfaces and composition summaries match
preview versus Apply. Terrain unchanged; post-Apply surfaces stable. This is a
post-previous-Apply state, not a fresh exact-baseline run. Tracing was disarmed.

Next discriminating execution context: managed/Burst-disabled research launch
versus the original game-native Burst execution. Do not conclude Burst is causal
without exact-baseline reproduction and captures. Scheduling-boundary capture
does not require managed worker frames; add an explicit opt-in to capture Burst
jobs, retaining actual Burst state in evidence. Also capture the three remaining
publication stages in the same bounded run to avoid repeated deployment cycles.
