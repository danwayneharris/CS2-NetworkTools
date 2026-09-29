# Game launch v2 handoff

Draft PRs published to personal forks: NetworkTools#5 (e82f033) and bridge#2
(1392542). Created dan/game-launch-v2 in both repositories from those checkpoints.
The pre-existing UI package-lock change remains unstaged and excluded from the PR.

Reviewed autonomous-game-lifecycle-investigation.md and
programmatic-mod-ui-control-investigation.md. Bridge local commit d12fa96 fixes
the startup helper's incorrect outer-package save identity. It now reads the
internal metadata CID; all five offline extraction tests pass. The saved regression
package confirms these identities differ. This does not validate actual launch.

Live bridge ping succeeds, controlEnabled=false, game still running. Left the
city untouched. No launch/load/save/control loop was performed, and the helper's
launch guard remains. The experiment needs a safe enabled-control checkpoint or
closed game before restart work. A typed mod-control adapter remains unimplemented.
Bridge notes document the exact offline work and limitations; these follow-up
commits are local and separate from the published PRs.

## Successful launch/load follow-up

User subsequently closed the game. The isolated Steam app-ID-hint experiment
started Cities2.exe and loaded the exact saved rail-regression metadata ID;
native logs confirm the named save. Bridge reports loading=false, gameMode=Game,
selectedSpeed=0 and controls disabled. Expected test mods initialized. One launch
and paused-load cycle is now verified; save, exit/repeat, and mod-control are not.
See sibling bridge launch-v2-experiment.ps1 and its v2 session note. No new
NetworkTools build/deployment or game-state mutation beyond loading was performed.
