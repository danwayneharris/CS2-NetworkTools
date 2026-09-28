# Programmatic mod UI research — 2026-09-28 11:18 PDT

Documentation-only investigation of the bridge dispatcher, installed UI binding APIs and Network Tools input paths. No live bridge/UI commands, process changes, builds, configuration changes or mod edits. Concurrent agent work must remain untouched.

## Findings and documentation

- Verified Game, Colossal.UI.Binding, Colossal.UI and cohtml.Net hashes match the local decompile.
- Current bridge has a fixed command dispatcher and no generic mod UI/script/binding invocation endpoint.
- Game binding registry is enumerable; native View exposes ExecuteScript. C# TriggerEvent sends toward JS, while JS engine.trigger invokes registered C# handlers.
- Traced existing Network Tools parameter/tool/panel/Apply inputs. Identified frontend-only React state, missing generic path-selection input, outbound-binding versus model-state distinction, and absent generic parameter bounds enforcement.
- Documented candidate forwarding adapter and recommended shared validated command API, including readiness/revisions and asynchronous completion.
- Wrote ../programmatic-mod-ui-control-investigation.md. Static inspection only; no build, live bridge command or game/UI mutation. New documentation is the only change for this task.
