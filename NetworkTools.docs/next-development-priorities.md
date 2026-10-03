Current priority index: [ROADMAP.md](ROADMAP.md) (planning baseline).
The saved planning response below remains historical.

**Historical plan; successor status October 1:** the generic-provider separation
and Python/MCP work have since been implemented in their respective products.
The saved response below is not the current task list. Use [audit disposition](audit-disposition.md)
for this correctness sprint, [provider contract](automation-provider.md) for the
integration boundary, and [Smooth Curve plan](smooth-curve-plan.md) for remaining
product decisions. Broad bridge roadmap work remains independently prioritized.

﻿# Next development priorities

Saved 2026-09-30 at Dan's request. The first section preserves the previous planning
response; the addendum records the subsequent loose-coupling discussion. This is
proposed work, not authorization to begin implementation or evidence it is complete.

## Saved planning response

**I'd put a bounded automation/performance investigation first, with a small MCP prototype immediately behind it.** That work could improve every subsequent development cycle—but I wouldn't turn it into a full bridge rewrite.

The strongest part of `cities2-agent-bridge-ndc/docs/MCP-FEASIBILITY.md` is its distinction between **a better agent interface** and **a faster testing loop**. MCP can provide typed, discoverable tools; it won't automatically accelerate native rebuilding. Our current regression runner already handles polling locally, but still starts `powershell.exe` for each bridge request. That gives us a concrete measurement target.

My recommended order:

1. **Measure and streamline the existing testing loop.**
   Time a representative select → preview → Apply → verification sequence. Separate client startup, bridge waiting, native work, and journaling where active. Start with coarse measurements; add detailed instrumentation only where it answers a question. Preserve the same verification requirements when comparing alternatives.

2. **Build a small external MCP adapter.**
   Follow the document's recommendation: a separate local process using the existing mailbox. Start with status, capabilities, selection state, and junction diagnostics; then add guarded selection, strength, splits, and Apply. Reuse a common client layer with the regression runner so we don't maintain two implementations of session checks and recovery. Success means a complete, verified experiment with less orchestration—not merely a successful tool call. Defer embedded HTTP, new IPC, and comprehensive command coverage.

3. **Close the most important regression gaps.**
   Add the non-merging rail crossing, broader road/lane configurations, and more demanding ramp/slip-lane selections. Include bounded vehicle-traversal checks where practical: connector preservation is valuable evidence, but not proof that traffic behaves correctly. Get your visual assessment of ordinary splits and interior-junction results.

   Also settle the millimeter-drift policy here. I wouldn't spend a sprint eliminating a harmless 3–5 mm native adjustment. We should distinguish authored-position preservation from native reconstruction drift, while continuing to check connectivity and unintended movement.

4. **Make junction validation ready for normal use.**
   Resolve the Debug/Release divide deliberately, validate Burst compatibility, and improve feedback when the tool is searching, constrained, or unable to preserve connections. This is also the right point for a focused cleanup of preview freshness, search state, and validation ownership—now that regressions exist.

5. **Implement junctions as split points.**
   Give these different semantics from ordinary degree-two pins: preserve intended connections and prefer the original **relative branch angle**, rather than forcing both sections onto one shared tangent. Test symmetrical merges without assuming a privileged through-route.

6. **Resolve the strength/side-bias UX, then implement it.**
   Your slider gripe belongs here. First choose whether the center means “original geometry” or “smooth with no side preference.” The existing plots provide a discussion aid; neither interpretation should silently become the design.

7. **Extend the same discipline to slope, then investigate combined smoothing.**
   Begin with slope regressions and measured grades. For a combined tool, keep horizontal alignment and vertical profile as separate solvers, recompute distance after horizontal changes, and validate their combined result. Terrain and obstacle routing remain later work.

For the **next autonomous sprint**, I'd scope it to **1–3**, with a small MCP prototype and concrete regression results as deliverables. That improves our ability to iterate before adding another layer of geometry behavior.

## Addendum: a generic mod extension contract before MCP

Dan questioned the maintenance cost of NetworkTools-specific code in the bridge.
Agree: the current integration is a useful prototype, not the intended extensibility
boundary. Insert a bounded generic-provider contract/migration step between the
initial measurements and MCP implementation. Do not reproduce hardcoded nt_* tools
in another layer and then have to generalize two integrations.

### Current evidence

- Bridge `src/NetworkToolsAdapter.cs:14-18` explicitly resolves the NetworkTools
  assembly, `NetworkTools.Automation.BridgeApi`, and `InvokeV1(string,string)`.
  This avoids a compiled reference but still hardcodes another mod's identity.
- Bridge `src/Mod.cs:162-184` knows the nt_* prefix, routes individual commands,
  and advertises them in read/control capability lists. `bridge.ps1:2` and
  `commands.json:56-62` duplicate the inventory.
- NT `Systems/Tools/RoadShape/RoadShapeToolSystem.Automation.cs:123-137` already
  owns the public entry point and checks city/paused state. Its earlier command
  implementation owns strength bounds, selection eligibility, revision/submission
  freshness and preview/Apply semantics. Keep those responsibilities in NT.
- The boundary is currently Debug-only. Generalizing discovery must report absent
  or unsupported providers explicitly, not infer readiness from loaded assemblies.

### Proposed responsibility split

Bridge owns generic transport, provider discovery/routing, game-thread dispatch,
control/STOP policy, city/session checks, shared-operation coordination and generic
error envelopes. Mods own namespaced command descriptors, argument/result schemas,
domain validation, actual handlers and completion semantics. MCP owns protocol
presentation and maps those descriptors to typed tools; it does not encode NT logic.

Illustrative flow (names are proposals, not existing commands):

    Agent / regression runner
      -> external MCP adapter or shared mailbox client
      -> bridge provider registry
      -> networktools provider inside NetworkTools
      -> the existing tool state machine

Define an opt-in, versioned provider contract with a stable provider ID, protocol
version distinct from mod version, command descriptors, capability/readiness data,
and an invocation envelope. Commands are namespaced (for example
`networktools.smooth.set_strength`); schemas include units/bounds and side effects.
Keep per-call city identity and NT revision/submission preconditions. Distinguish
accepted work, completed work and independently verified effects.

A first prototype can use a documented reflection convention and string/primitive
JSON envelopes, expanding the existing InvokeV1 idea to generic discovery plus
DescribeV1/InvokeV1. Only explicitly opted-in provider endpoints are discoverable;
never expose arbitrary classes/methods from installed assemblies. Cache discovery
with lifecycle invalidation; handle load order, unavailable/disabled providers,
city changes, disposal, duplicate IDs and incompatible versions explicitly.

An optional source helper can hide discovery/serialization boilerplate from mod
authors. A shared contract DLL is another option, but introduces assembly deployment
and version coordination; choose that deliberately rather than making the bridge
itself a mandatory assembly reference for every participating mod. Mods should
continue working when the bridge is absent. An independently packaged integration
shim can support a mod whose author does not want automation code in its main mod.

Provider-declared side effects guide policy but are not a security sandbox: installed
mods already execute in-process. The bridge must not route unknown operations as
read-only or let a provider bypass the controls policy through a command-name prefix.
Default uncertain commands to control-required; provider handlers still enforce
all domain invariants. Do not trust schemas as a substitute for runtime validation.

### Bounded acceptance and migration

1. Capture the current workflow timing before altering its transport.
2. Specify the provider contract and test generic dispatch with a minimal second,
   synthetic provider as well as NT. Adding its commands must require no bridge or
   MCP source edits. Synthetic tests do not establish native Unity correctness.
3. Migrate NT through the contract and rerun existing live regressions. Preserve the
   existing nt_* interface temporarily through clearly isolated compatibility aliases;
   document that those legacy aliases are the remaining deliberate coupling.
4. Build MCP discovery/typed tools from the same descriptors, including availability,
   stable names and bounded data. Handle provider changes/version mismatches explicitly.
5. Remove aliases only after callers migrate. Generalizing the registry must not
   weaken stale-result guards, uncertain-outcome handling, STOP or control checks.

Success means a mod author can ship a compatible provider in their own mod (or a
separate shim), and the unchanged bridge discovers and invokes it. It does not mean
arbitrary unmodified mods become controllable or all compatibility problems disappear.
No provider implementation or MCP integration was performed in this documentation task.
