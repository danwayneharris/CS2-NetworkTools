# Disabled runtime finishing compatibility candidate — 2026-10-03 01:15 Pacific

Implements the proposed candidate as reviewable, reversible source only. No new build
was deployed and no persistent correction was enabled in the running game. The pending
scope question still applies to activation on all native rebuilds.

## Code and ownership

NetworkTools owns an isolated Debug-only `Compatibility/FinishHeightCompatibility`.
Bridge remains unchanged. No Harmony attributes cause automatic installation; the
mod lifecycle hook checks explicit launch argument
`--nt-experimental-finish-height-preparation`. Default launch remains disabled.
Enabled installation requires the recorded Game, Mathematics and native Burst binary
SHA256 fingerprints. It refuses an existing GeometrySystem transpiler and validates
exactly one original finishing scheduling callsite and exact job-field types. Refusal
is logged and leaves the compatibility patch uninstalled. Disposal removes only its
own Harmony patch. Release contains no installation hook or compatibility class.

The wrapper schedules a non-Burst `PrepareHeightsJob` after the original dependency,
using the original deferred entity list, read-only height map and EdgeGeometry lookup.
Then it schedules the unchanged native Finish job with preparation as its dependency.
It does not synchronously Complete, read the deferred length early, allocate scratch
native containers, dispose vanilla-owned containers, or alter the global Burst setting.
Main-thread reflection metadata is cached per job type; worker performs only map reads
and component writes. Pass counter and first-schedule log do not imply job completion.

The eight field assignments are shared with NativeReplay via a linked source file;
no duplicate test implementation. The offline helper moved into Mod/Compatibility.

## Verification and failed attempt

Non-deploying `dotnet msbuild ... -t:Compile -p:Configuration=Debug` passed. Existing
project warnings remain; no full SDK/postprocess/UI build claimed.

Extended the existing actual-IL scheduler test runner instead of creating a parallel
rewriter test. `--finish-compatibility-tests` loads the compiled production assembly,
rewrites the installed GeometrySystem's actual call operands and verifies exact generic
parameter/return signature. Missing and duplicate finishing sites reject. All three
changed fingerprints reject and the known triple accepts. Default-off Install leaves
Status disabled. It does not patch the host or invoke Unity scheduling.

First default-off test failed with `SecurityException: ECall methods must be packaged
into a system module`: merely JIT compiling the combined activation/installation method
resolved a Unity-only API outside Unity. Separated flag inspection from the enabled
installation method (explicit NoInlining). Default-off test then passed. This diagnoses
an offline-host boundary, not evidence the original game was broken.

Re-ran the five-case cohort using the shared helper and the discriminating-case gate.
See compact result beside this note and artifacts/finish-shared-preparation-cohort-01.
Previous 18 replay negative contracts passed after adding the preparation mode; they
were not rerun solely for relocating the unchanged assignments.

## Not yet established

No live scheduling/native-container lifetime, actual worker execution mode, performance,
reload, repeated rebuild, lane preservation or visual evidence exists for this runtime
candidate. No runtime patch ordering with other transpilers is supported. In particular,
the existing Bridge research trace also wraps this scheduler: do not arm it simultaneously
and claim combined instrumentation works. Its callsite guards should reject overlap;
interoperability remains an explicit follow-up before instrumented combined use.

Before enabling: obtain Dan's pending all-native-rebuild scope decision, then preserve
toy checkpoint, build/deploy/restart with explicit opt-in and verify runtime status.
Test native surfaces, lanes, ordinary edit rebuild and saved checkpoint reload against
fresh identities. Measure actual preparation cost; pass counts are not timing evidence.
The goal's draft PR remains incomplete until that verification or a documented blocked
handoff; do not describe the offline candidate as a durable working game fix.
