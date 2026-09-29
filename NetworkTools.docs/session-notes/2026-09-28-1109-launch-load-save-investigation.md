# Launch, load, and save investigation — 2026-09-28 11:09 PDT

Scope: documentation-only inspection of installed executable/launcher and matching decompiled assemblies. No game launch, save mutation, configuration change, or mod-code change.

Initial findings: setup record locates the existing full decompile. Bridge INSTALL.md records an earlier failed direct launch and an inspection-only startup helper. This session will independently trace executable identity, startup options, save/load control flow, and automation limitations.

Environment: sandbox shell lacks git/rg on PATH and does not expose the owner's CSII variables. Git located in cmder's bundled installation; source and installation paths obtained from setup.md. PowerShell Select-String is the search fallback.

## Completed investigation

- Verified Game.dll and four relevant Colossal assembly hashes against the existing ILSpy manifest.
- Inspected native PE headers/strings: Cities2.exe is the Unity player entry point; dowser.exe is the launcher bootstrap. Native files have no CLR header, so ordinary C# decompilation is inapplicable.
- Traced Steam restart/init, startup flags, AutoLoad, saved-game serialization, bridge controls and QA client.
- Found the existing startup helper uses the outer package CID, whereas AutoLoad requires a save metadata/data identity. Read-only inspection of a test save confirmed distinct package and metadata GUIDs. No helper change made.
- Found GameManager.Save can report failure via its event yet return true; the bridge trusts that return. No source change made.
- Confirmed pause-after-loading is a game setting defaulting false, and bridge controls reset on preload.
- Recorded historical launch failures separately from this session's static evidence. No runtime experiment performed. User reiterated no launch/kill due to a concurrent agent; honored throughout.
- Produced ../autonomous-game-lifecycle-investigation.md with evidence, limitations and a proposed acceptance sequence. Only this session note and the new report belong to this change.
