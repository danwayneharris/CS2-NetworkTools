# Native capture findings — 2026-10-02 03:26 PDT

Goal remains active. Dan confirmed all other agents are paused and explicitly
authorized taking over the debugger and restarting the game. Reproduction and
learning remain the target; a separate causal explanation is not required.

## Preserved state and runtime

- Checkpoint save: CitiesIIAgentBridge-offline-geometry-research-handoff-20261002-0948-20261002-094741-b7365aed.cok.
- Save operation 4162ee59eded496189ff00128ddf4c97 completed; file SHA256
  041446DF549599A9E99AC51295A4E1AF32168E61C2002285302F1AB1B7737F54.
- Stale debugger owner was UnityDevTools MCP 1.2.0, dotnet PID46288. Stopped only
  that confirmed debugger process with Dan's authorization. One retry still failed
  before execution, so gracefully closed Cities2 via CloseMainWindow; no force kill.
- Deployed Bridge 08b874e research build, DLL SHA256
  DBA09ADDC4BDBEC1F30783A15F7B33AB42A910DE99BE567675FDDE026E71D9D8,
  with matching PDB and verified backups. NT remains the recorded 04792dd runtime
  hash 48EF3D8D36626D73C61F80133850509F6E8688337F05CF185533225636D0AEE9.
- Relaunched the exact 070428 failing baseline (SHA256 FB4E5F5628BD4B8EE7B1FBEE75C2EDA77B14A8B148EC409DB91DBF5CA5AED376),
  metadata ID92f050b665f6ca11804af01b61c8ef6e, with developer/UI flags and
  --burst-disable-compilation. The outer .cok.cid is NOT the save metadata identity;
  the established package-metadata extractor verifies the correct launch ID.
- New PID45892, transport a1c3cf522d4d4841b835215108ea0cfd, citySession
  92099620a3114491b9369126b8e38362. Wantagh/population0/speed0/controls true verified.
  BurstCompiler.IsEnabled returned false at a managed Bridge.Tick breakpoint.

## Captures and failures

Bridge research branch now 45e82bc in
cities2-agent-bridge-ndc/artifacts/offline-geometry-capture. Its docs and session
notes describe the capture code, API tests, deployment and failed approaches.

The synchronized raw closure capture returned 210 entities in approximately115ms.
Five NetGeometryData records could not serialize embedded EntityArchetype native
pointers; the response correctly reports incomplete. A separate successful helper
capture records80 scalar/value fields across those five prefabs, excluding archetype
handles. These are separate observations, not falsely labelled one atomic snapshot.

Native InitializeNodeGeometry entry helpers captured six temporary-node roots and
111 entities. The same five prefab gaps remain. No paired exit capture succeeded.
Root IDs were rediscovered from the live network before capture.

Debugger experiments:

- Long interactive holds caused preview rejection; classify these as diagnostic.
- Auto-resuming conditions invoking the capture helper failed to resolve this.
- Return-offset breakpoints did not yield paired outputs; step-out returned
  INVALID_ARGUMENT. These do not establish native geometry behavior.
- Unconditional CalculateEdgeGeometry and FlattenNodeGeometry entry breakpoints
  did fire. Invoking the helper at CalculateEdgeGeometry failed before execution
  because the main thread remained in native code waiting for jobs.
- No errors were found in Player.log beyond the ordinary shader name containing
  InternalError. The invocation limitation does not justify claiming game failure.
- Removed all breakpoints and resumed every debugger-held pause. Latest status:
  attached=true, heldSuspends=0, paused=false, breakpoints empty. Game remains open.

Evidence remains local: Bridge artifacts/research-debug contains baseline-raw-closure,
baseline-prefab-fields, preview-initialize-entry-1 and native-execute-offsets JSON.
Raw helper files are under LocalApplicationData/CitiesIIAgentBridge/geometry-research.
NT artifacts/offline-research/live-handoff-0948 retains checkpoint/runtime records
and debugger-observations.json, including the failed condition and step results.
Separate native-preview/native-init/native-stage-chain directories retain request
and response evidence. No Apply or production geometry fix occurred.

## Executable workflow change

Added --no-wait to prepare-surface-preview-replay.py. It retains baseline geometry
validation and returns after selection acknowledgement, explicitly making no
preview-readiness claim. This lets a debugger driver own subsequent observation
without an unrelated state poll timing out while the game is intentionally stopped.
Python compilation passed; several live selections returned the expected explicit
acknowledgement. Existing default readiness polling remains unchanged.

## Next useful implementation

Move capture around the original scheduling calls in GeometrySystem.OnUpdate.
Complete explicit dependency handles before pre/post observations while retaining
the actual native scheduled jobs and their real query/list membership. This adds
serialization and must be labelled diagnostic; compare ordinary behavior separately.
The scheduling source is GeometrySystem.cs:3402 onward, especially the native chain
near3567. Chunk scheduling uses JobChunkExtensions.ScheduleParallel<T>; deferred
edge scheduling uses IJobParallelForDeferExtensions.Schedule<T,U> with a NativeList.
Capture copies of deferred job arrays must be resolved from the completed schedule
list without modifying the original job passed to native scheduling.

Installed NT already supplies Harmony2.2.2. Any research patch must have its own
owner ID, a strict installed-assembly/call-site gate, bounded opt-in capture, and
must leave other patches intact. Fix archetype serialization using its public
component-type representation instead of following private pointers. Do not keep
repeating debugger invocation at worker stops: the limitation is now evidenced.

The offline anchor, simpler control and held-out differential criteria remain
unmet. These captures advance the input inventory; no live checks are retired.
