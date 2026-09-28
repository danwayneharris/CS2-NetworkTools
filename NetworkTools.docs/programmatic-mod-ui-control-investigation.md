# Programmatic control of code-mod UI through the agent bridge

Research date: 2026-09-28. Documentation only; no live commands, UI attachment, builds, process changes, settings changes or mod edits.

## Answer

**Yes, the game has the mechanisms needed to programmatically invoke many code-mod UI inputs. The current Cities II Agent Bridge does not expose a generic command for doing so.** A bridge extension could forward known UI bindings without changing the target mod. For dependable automation of our own mod, a small typed command API shared by the UI and bridge would be stronger.

There are three different capabilities to distinguish:

| Capability | Current evidence |
| --- | --- |
| Invoke a mod's existing C# UI handler | Supported by the installed game/Coherent binding architecture; a bridge-to-UI adapter is not implemented in the inspected bridge. |
| Set every visible UI control in arbitrary mods | Not universal. Some state is frontend-only; bindings and argument formats vary by mod. |
| Run a reliable select -> configure -> preview -> Apply workflow | Needs explicit selection, validation, readiness and completion contracts. Invoking a button handler alone does not provide them. |

The best initial target is Network Tools parameter control on an already selected path. A complete autonomous tool workflow is a larger step, especially selection and preview/apply synchronization.

## Evidence baseline

Local decompile location from `C:/Users/danwa/.cs2-modding/setup.md`:
`C:/Users/danwa/dev/cs2-mods/cs2-decompile/src`.
In this report `D/` denotes that directory, `B/` denotes sibling `../cities2-agent-bridge-ndc`, and unprefixed paths are in this repository.

The installed game is Windows Steam 1.6.2f1, Unity 2022.3.71f1. Installed assembly SHA256 values were checked against the 2026-09-28 ILSpy 9.1.0.7988 manifest:

| Assembly | SHA256; all matched |
| --- | --- |
| Game.dll | `AAEE15C4FA41C130ABAA1183840667E4FAAE531D67E8A9FA7536618EFFA2F86A` |
| Colossal.UI.Binding.dll | `395E820A903F13CDFB7A3793145CCE2003CDB90C524C4C704E8FDD91FBEAA044` |
| Colossal.UI.dll | `65F24D7195D2311AC8DB47014C5573FD9631EAD4103B210EE5BF100BBDC6D5C6` |
| cohtml.Net.dll | `3ACF1A7C76AED0DF256F6C88D1B586ADBF45FCE9FCA673A51BFFF4B290EF0B53` |

Repository HEADs observed during reading: Network Tools `e14575b`, bridge `b8e35ec`. Another agent is working concurrently, so these are observations rather than a locked snapshot. References describe files read during this investigation. No other agent's changes were staged or altered.

The UI bundle has no recorded readable copy. This investigation uses installed C# assemblies, this repository's frontend code/type declarations, and Coherent's primary documentation. It does not claim to have audited the entire minified game frontend or live endpoint behavior.

## 1. What the bridge currently offers

`B/src/Mod.cs:150` dispatches a fixed list of commands. `get_capabilities` declares command groups around lines 160-184, and unknown commands are rejected in the switch. The inspected implementation has no generic `click`, `set_mod_parameter`, `invoke_ui_binding`, `execute_javascript`, or equivalent command.

`B/src/Mailbox.cs:12,20-29,44-97` implements a local JSON file mailbox, dispatching requests through a callback. It checks protocol, session, citySession and expiry before dispatch, and retains responses to avoid redispatch after publication trouble. This is a custom RPC-like transport, not a game-provided mod-UI automation service.

Important distinctions:

- `get_selected` reads the game's selected entity (`Mod.cs`, switch case). It does not set a Network Tools path selection.
- `get_junction_preview` inspects temporary network geometry. It does not manipulate sliders or press Apply.
- `batch_execute` runs an allowlisted command set (`B/src/Workflow.cs:26`); it does not invoke arbitrary methods.
- Loaded assemblies listed in capabilities are explicitly marked `assembly_loaded_not_health_verified`; they are not proof of mod-control integrations.
- The bridge has a few internal uses of reflection, but no arbitrary-reflection command exposed to the client.

A source search covered the bridge's C# files for UI registration, script execution, binding registry access and NetworkTools references. Combined with dispatch inspection, this establishes absence of a generic UI route in the inspected checkout. The installed bridge DLL was not interrogated, and source checkout state must not be confused with deployed/live capabilities.

The bridge's existing dispatcher can pause simulation for commands that are not classified as status-only (`Mod.cs:153-159`). Future read-only binding discovery/status commands must be classified deliberately if they are intended to remain observational. This research sent no mailbox requests.

## 2. Game-supported binding architecture

A mod's React code normally communicates with C# through named bindings. The types in `D/Colossal.UI.Binding/Colossal.UI.Binding/` provide:

| Kind | Direction and purpose | Relevant source |
| --- | --- | --- |
| ValueBinding / GetterValueBinding | C# state observed by frontend | `ValueBinding.cs`, `GetterValueBinding.cs` |
| TriggerBinding | Frontend invokes a C# callback; no return value | `TriggerBinding.cs:10-46` and generic variants |
| CallBinding | Frontend calls C# and receives a result | `CallBinding.cs:6-59`, `RawCallBindingBase.cs:23-28` |
| CompositeBinding | Contains/registers child bindings | `CompositeBinding.cs:10-56` |

`BindingBase.cs:14-34` exposes group, name and path, with path constructed as `group + "." + name`. `Game.UI/UISystemBase.cs:47-57` adds bindings to `GameManager.instance.userInterface.bindings`. `Game.SceneFlow/UserInterface.cs:30,46` publicly exposes the UI view and binding registry.

This is real game infrastructure, not simulated mouse input. It does not amount to a supported universal external-agent protocol: the bridge must provide transport, discovery policy, validation and acknowledgements.

### Direction matters

A C# `TriggerBinding` attaches using `view.RegisterForEvent(path, callback)`. From JavaScript, `engine.trigger(path, ...arguments)` invokes that handler. By contrast, C# `View.TriggerEvent` sends an event toward JavaScript listeners; calling it with a C# binding's name is not a substitute for invoking the registered C# handler. Coherent documents these two directions separately. [Coherent Gameface Unity binding documentation](https://docs.coherent-labs.com/unity-gameface/integration/ui_scripting/js_binding_unity/).

The installed `D/cohtml.Net/cohtml.Net/View.cs:344-346` exposes `ExecuteScript(string)`. The public object chain is:

```text
GameManager.instance.userInterface.view.View
```

`UserInterface.view` returns Colossal.UI.UIView, whose public `View` property exposes the native wrapper (`D/Colossal.UI/Colossal.UI/UIView.cs:152`). This means an in-process bridge extension could run a fixed script that invokes `engine.trigger`, or install a frontend relay and send structured events to that relay. This route does not inherently require the external UI debugger to be enabled.

**Feasibility, not runtime verification:** ExecuteScript returns void. Its existence proves an execution entry point, not that a particular invocation has reached the correct view, succeeded, or finished a game operation. The view must be ready, the binding attached, and invocation scheduled in the game's update context. Do not call Unity/UI APIs from a mailbox reader's arbitrary background thread or block the main thread waiting for a response that itself requires another frame.

### Discovery is possible, but incomplete

`IBindingRegistry` extends `IBindingGroup`; `IBindingGroup.cs:7` exposes `IEnumerable<IBinding> bindings`. `CompositeBinding.cs:24` provides that enumeration. A future bridge can walk groups and inspect `BindingBase`/`IDebugBinding` metadata to list names, attachment status and available kind information.

This is not a self-describing schema of every control:

- `IBinding` only specifies attachment lifecycle, not a universal Invoke method.
- Trigger delegates are private, and generic trigger Callback methods are protected.
- Binding kind metadata can be imprecise: RawCallBindingBase reports Unknown.
- Generic argument types do not describe custom readers, legal ranges, units, prerequisites or side effects.
- Arbitrary JavaScript-only controls have no C# binding to enumerate.
- Nested groups, duplicates and custom implementations need bounded traversal and explicit handling.

Reflection into private delegates might be possible, but it couples the bridge to implementation details and bypasses the normal argument reader. Public shared commands or a known frontend binding route are preferable.

### Calling does not acknowledge completion

TriggerBinding catches callback exceptions and logs them (`TriggerBinding.cs:24-35,67-81`). A successful script submission or trigger return does not prove acceptance. Value changes, preview generation, Apply and network rebuilding can all happen at different times.

CallBinding gives a response boundary, but its callback is a synchronous Func returning TResult. It is not automatically a long-running operation protocol. Return an operation ID for asynchronous work and expose separate status/result readback.

## 3. Concrete existing Network Tools inputs

The UI group is `NetworkTools` (`NetworkTools.Mod/UI/mod.json`). CommonUISystemBase creates names prefixed with `BINDING:` for state and `TRIGGER:` for mutations. See `NetworkTools.Mod/Common/Systems/CommonUISystemBase.cs:43-103` and `Common/ui/utils/bidirectionalBinding.ts:9-28`.

| UI operation | Existing trigger name within NetworkTools group | Arguments / readback |
| --- | --- | --- |
| Open/close panel | `TRIGGER:PANEL_OPEN` | bool; `BINDING:PANEL_OPEN` |
| Select tool | `TRIGGER:SELECT_TOOL` | tool prefab ID string; inspect current tool/UI data afterward |
| Set smoothing strength | `TRIGGER:roadShape.smoothingFactor` | number in native range 0..1; `BINDING:roadShape.smoothingFactor` |
| Change RoadShape mode | `TRIGGER:roadShape.template` | enum integer; discover current allowed values |
| Reset parameter | `TRIGGER:RESET_PARAM` | exact parameter key |
| Reset active tool parameters | `TRIGGER:RESET_TOOL` | no arguments |
| Request Apply | `TRIGGER:REQUEST_APPLY` | no arguments; separate apply/readiness state needed |
| Change snap/target/view flags | `TRIGGER:SELECTED_SNAPS`, `TRIGGER:SELECTED_TARGETS`, `TRIGGER:SELECTED_VIEWS` | integer bit flags |
| Select a network prefab | `TRIGGER:PS:SELECT` | parameter key and Entity value; see frontend trigger declaration |

Sources: `Systems/UI/UISystem.cs:86-109,170-198`; `UI/src/gameBindings.ts` GAME_TRIGGERS; `Systems/Tools/RoadShape/RoadShapeToolSystem.cs:24-48`.

Illustrative frontend code, **not executed**:

```ts
import { trigger } from "cs2/api";
trigger("NetworkTools", "TRIGGER:roadShape.smoothingFactor", 0.65);
```

Equivalent candidate in the ready Coherent JavaScript context:

```js
engine.trigger("NetworkTools.TRIGGER:roadShape.smoothingFactor", 0.65);
```

The imported `cs2/api` functions are declared in `UI/types/api.d.ts:33-42`. A debugger console is not necessarily an ES-module context, so do not assume an `import` statement or the mod's own TypeScript exports are globally accessible there.

### Why the existing parameter path is useful

`UISystem.cs:170-182` connects float/int/bool/enum input callbacks to the actual parameter Value and republishes OnChanged values. `Parameters/Parameter.cs:18-28` raises change notification on assignment; `Base/BaseToolSystem.Parameters.cs:97-115` subscribes to invalidate work and synchronize handles/dependencies.

Thus calling the existing trigger can exercise the same semantic edit path as the UI. A bridge adapter could also access the public `Parameters`/`ParametersByKey` collections (`BaseToolSystem.Parameters.cs:53-74`) and set a known typed parameter through a validated public command.

**Do not merely mutate the outbound ValueBinding.** `Common/Extensions/ValueBindingHelper.cs:16-18` updates display state, whereas `UpdateCallback` at lines 31-34 also invokes the application callback. Updating a display binding alone can make a slider look changed without modifying tool behavior. Similarly, a TypeScript TwoWayBinding wrapper does not prove that the C# side registered a setter: the two-argument CreateBinding overload registers only the value binding.

### Existing limits and concrete failure cases

1. **Bounds are metadata, not enforced by the generic setter.** FloatParameter stores Min/Max; Parameter.SetValue assigns directly, and EnumParameter.IntValue casts without validating membership. A bridge could send a value the visible slider would not permit. Validate finiteness, bounds, enum membership and bit masks in a shared command layer.
2. **Native values differ from displayed values.** Parameter fields apply displayScale and distance-unit conversion (`UI/src/components/toolActionPanel/shared/parameterField.tsx:130,160`). Do not send displayed percentages/meters/units without consulting the parameter schema.
3. **Tool selection is not path selection.** SELECT_TOOL activates a prefab tool. INodeSelectionProvider exposes only GetSelectedNodes; SELECTED_ENTITIES is an observation binding. No general UI setter for an arbitrary selected node path was found in the inspected registration list. Programmatic path selection needs a dedicated adapter respecting topology, entity versions and selection lifecycle.
4. **Apply has a guard that must be retained.** `UISystem.Handlers.cs:49-58` calls GetCurrentApplyState and only forwards when Enabled. A future public API must share that check; directly calling an exposed tool RequestApply method is not established as equivalent to the guarded UI handler.
5. **Some UI state has no C# endpoint.** `prefabSearchPanel.tsx:28` holds searchQuery in React useState; `prefabSearchContext.tsx:20-21` holds open/active-key state; `toolActionPanel.tsx:38` holds tutorial visibility. Calling backend bindings cannot universally control those states. Add a frontend command handler or stable DOM automation hooks if these are test targets.
6. **Edits can persist settings.** Selected snaps, targets, views and Anarchy handlers call ApplyAndSave in `UISystem.Handlers.cs`. A generic UI mutation endpoint must not present these as harmless display-only operations.

## 4. Extension options

| Approach | Changes needed | Strength / limitation |
| --- | --- | --- |
| Known-binding forwarding through ExecuteScript | Bridge code plus optional frontend relay; target mod may stay unchanged | Reuses current UI handlers; needs schemas and readback; frontend availability required. |
| Typed Network Tools adapter | Bridge adapter and a small public API in Network Tools | Strong validation, state and completion contracts; best fit for repeatable geometry tests. |
| Shared opt-in mod automation interface | Small contract assembly/registration mechanism plus participating mods | Scales across cooperating mods; requires versioning and one consistent runtime contract identity. |
| Mod-local mailbox/API | Target mod only | Can make that mod automatable without bridge changes, but duplicates transport/session logic and is not control through the existing bridge. |
| Coherent debugger or DOM automation | External UI controller and enabled debugger/appropriate native tooling | Useful for frontend-only behavior and visual tests; version/focus/render dependencies; not an existing bridge command. |
| Private reflection patches | Mod-specific brittle adapter | Possible fallback, not a general supported game contract. |

### Smallest useful bridge experiment

Add a narrow, named command such as `networktools_set_parameter` with a fixed allowlist, rather than exposing unrestricted JavaScript. Internally forward to the known binding or typed adapter. Serialize names/arguments as data; never concatenate arbitrary user text as executable JavaScript. Return accepted/rejected and observed state, not merely `script_submitted`.

For a frontend relay, use a fixed C# -> JS event whose installed JS listener calls the specified known C# trigger and reports the request ID back. Remember that the callback's return only acknowledges dispatch; wait for authoritative value/revision status separately. Subscribe before sending a mutation so an immediate update is not missed. Balance subscriptions and remove handlers on navigation/disposal.

### Recommended addition to Network Tools

Factor application operations into one small command/service layer called by both existing UI handlers and an automation adapter. This is the **ports-and-adapters pattern**: React bindings and the agent bridge are two inputs to the same validated behavior.

Proposed API surface (names are design suggestions, not existing methods):

- `DescribeCapabilities`: API version, parameter types/native units/ranges, supported tools and operations.
- `GetState`: active tool, selected entity IDs/versions, parameters, input revision, preview revision, readiness and rejection reason.
- `TrySetParameter` / `TrySetParameters`: validate mode, value, units and expected revision; commit edits through existing parameter objects.
- `TrySelectPath`: validate identities/connectivity and use the same selection lifecycle as manual input.
- `RequestApply(expectedPreviewRevision)`: reject stale/not-ready state and return an operation ID.
- `GetOperation`: report submitted, preview-ready, applying, completed or failed based on actual lifecycle evidence.

Do not advertise multi-parameter editing as atomic unless it validates everything before mutation and suppresses intermediate notifications appropriately. Parameter changes currently trigger dependency propagation immediately.

The bridge can discover an opt-in provider by an explicit registration API or a carefully versioned shared interface. Compile-time references are simplest for a dedicated adapter; optional providers should fail clearly when missing/incompatible. Merely finding an assembly or matching a type name is not proof of a compatible service.

This can be implemented in Network Tools itself and the bridge without changing the pinned Common submodule. A generic mod ecosystem contract can come later if the single-mod experiment proves useful.

## 5. Runtime coordination and scope

Bindings remain attached independently of whether a UI system's OnUpdate is enabled: `UISystemBase.OnGamePreload` sets Enabled, while teardown removes bindings only in OnDestroy. Therefore the presence of a binding is not permission or proof that it is valid in the current game mode. Each mutation command needs explicit loaded-city/tool/mode checks.

Preserve the bridge's control checkbox, STOP behavior, request expiry and session invalidation. UI parameter/Apply commands are mutations even if described as manipulating controls. Reject commands during an incompatible active operation. The game does not automatically apply bridge authorization checks to arbitrary UI triggers; an external debugger route would bypass that bridge policy unless the controller explicitly enforces it.

Use the existing update-thread handoff: the bridge registers its Tick updater with GameManager, and Tick pumps the mailbox (`B/src/Mod.cs:54,90-106`). Return promptly and observe later frames instead of sleeping or waiting synchronously inside a binding callback. Preview jobs, entity-command-buffer playback and lane rebuilding require distinct completion evidence.

A future automation session must also coordinate with human/other-agent edits. Expected state revisions allow stale commands to fail instead of applying to a different selection. The current concurrent work is one reason this investigation makes no live calls.

## 6. Mod options panels and frontend-only controls

The game's generated widget system has setValue and invoke bindings too: `D/Game/Game.UI.Widgets/SettableBindings.cs:10-27` resolves a widget path and calls ISettable.SetValue; `InvokableBindings.cs:10-25` calls IInvokable.Invoke. These can support generated mod settings pages, but only after discovering the actual group, widget path, value type and current page lifecycle. They are not a universal property-name setter for all mods.

For React-only controls, expose a small frontend API using named commands tied to component state, or add stable test attributes and use the UI engine's automation facilities. Assigning an input's DOM value alone is not established as equivalent to React onChange or the mod's intended action.

A semantic adapter is best for changing game/tool behavior. UI-driven tests remain valuable for verifying that the visible slider, localization, units, disabled button and layout work correctly. Success through a typed backend API does not test those visual behaviors.

## 7. Suggested later validation

No implementation or live test was performed here. A bounded first experiment could:

1. Verify deployed bridge/game/mod fingerprints and discover the exact binding/schema.
2. On an authorized disposable, paused city with an already selected path, read the current smoothing factor and revision.
3. Set one in-range factor through the proposed route; verify actual parameter readback and eventual preview correspondence.
4. Test rejection of out-of-range/non-finite values, stale city/selection revision, unavailable mod, loading state and disabled controls.
5. Request Apply only for the acknowledged current preview and verify the resulting geometry separately.
6. Confirm reload/navigation cleans up subscriptions and no duplicate handler causes double application.

Research result: **the underlying mechanism is already present, and Network Tools already exposes many useful inputs. The missing piece is a bridge adapter and a reliable command/result contract, not simulated mouse dragging.** Universal control of arbitrary mod UI remains mod-dependent.

