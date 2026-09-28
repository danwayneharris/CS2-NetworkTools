# Restore previous deployment for a reproducible save

The experimental Debug junction search had already deployed when the user asked
to pause. The user closed the game and requested restoration of the earlier
behavior to recreate and save a working junction before applying the failing fit.

Created an isolated detached worktree at commit 78a3c83 under the user's temporary
directory (`nt-before-junction-search-78a3c83`), initialized the pinned Common
submodule from the existing local clone, then ran its bootstrap Debug build.
The current working tree and uncommitted search implementation remain intact.

Build/postprocessing/UI/deployment completed: 33 MSBuild warnings, zero errors;
webpack completed with three warnings. Installed NetworkTools.dll matches the
isolated build's DLL SHA256:
`AD3E7E8370806CB6B3306E04381FFD47FF43E569542CD4E881BD992521CA5EEB`.
This is a rebuild of the previous source, not a byte-identical archival binary.

The installed mod now has the previous behavior, without native candidate search.
Next: recreate the case, save before Apply, leave the failing preview selected for
capture, then use the same saved baseline for the experimental build. No game
launch, save, or Apply was performed by the agent.
