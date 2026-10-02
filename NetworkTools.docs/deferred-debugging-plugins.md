# Debugging-plugin recommendation (historical)

**Status update October 1:** the installation/connection investigation has moved
to the bridge's setup documentation; the original `add now` and machine-state
statements below are historical. Consult [bootstrap guidance](../BOOTSTRAP.md)
and the bridge's current instructions before changing an installation. Connection
or type-discovery success does not establish UI interaction or geometry behavior.
The suggested UI-plus-bridge comparison remains useful future test work; see
[audit disposition](audit-disposition.md). Do not reinstall from this old recommendation.

ï»¿**Yesâ€”Gameface first, Unity Devtools as a second step.** They fill gaps in our current workflow.

| Tool | Value for us | Recommendation |
|---|---|---|
| **Coherent Gameface** | Inspect React/DOM state, operate buttons and sliders, capture UI screenshots and JavaScript errors. Lets us test the actual player-facing controls, including the editor button we just fixed. | **Add now.** |
| **Unity Devtools** | Inspect live C#/ECS state, query components we havenâ€™t exposed through the bridge, and capture the rendered game frame. Could reduce diagnostic rebuilds and screenshot handoffs. | **Worth a controlled trial.** |
| **Our bridge** | Repeatable, checkpointed regression operations with tool-specific readiness checks and structured evidence. | **Keep as our primary regression runner.** |

Those capabilities are documented in the [Gameface releaseâ€™s README](https://github.com/CitiesSkylinesModding/agents-plugins/blob/coherent-gameface-v1.2.2/plugins/coherent-gameface/README.md) and [Unity Devtools releaseâ€™s README](https://github.com/CitiesSkylinesModding/agents-plugins/blob/unity-devtools-v1.2.0/plugins/unity-devtools/README.md).

The setup difference matters:

- **Gameface:** your Node version qualifies. The UI debug endpoint didnâ€™t respond during my check; weâ€™d enable `--uiDeveloperMode` on the next launch.
- **Unity Devtools:** requires .NET 10â€”you currently have SDK 8â€”and a development Mono player/debugging setup. The documented CS2 procedure replaces `UnityPlayer.dll` with the matching development version and adjusts boot configuration. That deserves a separate, reversible setup step.

My proposed first experiment: **use Gameface to operate an NT slider through the actual UI, then use the bridge to verify the resulting preview and Apply.** That would add meaningful end-to-end coverage without expanding the bridge.

Nothing has been installed or changed yet.
