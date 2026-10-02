# Scheduling tracer validation — 2026-10-02 03:42 PDT

Added `--schedule-transpiler-tests <research Bridge DLL> <installed Managed dir>
<new report>` to NativeReplay. The test invokes the actual compiled Bridge
transpiler against call operands decoded from installed GeometrySystem.OnUpdate.
It does not patch Unity or allocate native containers. Exact closed signatures
and generic arguments match for all five substitutions; missing/extra scheduling
sites are rejected. Both negative cases use controlled exceptions, not crashes.

Standalone build passed without warnings. Test passed; local evidence is
`artifacts/offline-research/schedule-transpiler-01.json`. Live capture and native
differential acceptance remain outstanding. Feature work stays paused.
