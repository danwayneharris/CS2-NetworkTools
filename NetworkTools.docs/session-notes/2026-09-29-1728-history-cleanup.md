# Archived squash-history migration — 2026-09-29 17:28

Dan authorized archiving every original PR history, squash-merging the remaining
reviewed PRs, and rewriting his earlier mainline PR merges with their complete
PR titles/descriptions. Upstream history is out of scope. Preserve user working
changes and use an isolated audit branch.

The phased script stores GitHub metadata and old/new SHA mappings alongside this
note. Each replacement uses the original merge tree; tree object equality proves
all tracked file contents, modes and submodule references are identical at each
checkpoint. This validates history repackaging, not new game behavior. Annotated
remote archive tags must be verified before rewrites. Main pushes use explicit
expected-SHA leases; an unexpected remote move stops the operation.

Bridge PR 3 was merged into the branch integrated by PR 4. Preserve its original
merge under a tag and include its title/description in PR 4's replacement message;
do not apply its changes twice. GitHub historical PR metadata is not rewritten by
changing Git history; the records continue to refer to archived original merges.

## Results and blocker

All archive tags were pushed and verified. NT PRs 7, 8, 9 and 10 were squash-merged
through GitHub after restacking their exact reviewed trees. Every resulting tree
matched its pre-squash head. The original open-PR heads and resulting GitHub squash
commits each have archive tags. Both main tips before rewriting also have backups.

The offline rewrite produced ten NT checkpoints and four bridge checkpoints. Each
replacement tree equals its original merge tree, and both final diffs are empty.
Bridge PR 3's full description is retained inside replacement PR 4's message.
Upstream history is unchanged.

Publication STOPPED: GitHub rejected the NT main force-with-lease push with GH013,
'Cannot force-push to this branch'. This is a repository rule, not a failed tree
check or an automatic tool-approval rejection. No protection setting was changed.
The bridge main rewrite was not attempted after that rejection. Both verified
candidates are preserved under review/squashed-main-20260929 for inspection.

At the stop: NT main is 9592f5edd3344a14b6538f13244974b332a4e520 (all four new
PRs merged); bridge main is db97a289621db3f0a9c7be012d14d0cf9912533d. Neither
older main history was rewritten. Candidate tips and per-checkpoint mappings are
in history-cleanup-20260929/*-rewrite.json. To continue, the owner must explicitly
arrange a permitted force-push under the repository rules; then revalidate remote
heads and archive backups before using the publish phase. Do not delete/recreate
main or alter protections to circumvent the rejection.

No working-tree edits were discarded; no build, deployment or game action occurred.
GitHub historical PR merge metadata will continue referencing the archived original
commits even if the candidate histories are later published.

## Completion after ruleset update

Dan updated the repository rulesets. Publication rechecked both remote tips, exact
tree equality and remote backup tags before explicit expected-SHA force-with-lease
pushes. Both succeeded:

- NetworkTools main: 9b1e61a790ca988cd2a91bc76b04e989c2c730b8
- Bridge main: 11bd0e5a65756bde61978ab3bf3231e63683e4b8

Local main refs equal origin/main and track the respective fork. Independent
post-publication verification passed for all 14 checkpoint trees, single-parent
commits, and full PR titles/descriptions including nested bridge PR 3. The first
message comparison falsely failed because its verifier read UTF-8 JSON with the
Windows default encoding; explicitly reading UTF-8 fixed the comparison. Published
commit messages were correct and required no further rewrite.

Upstream history and remote archive tags remain intact. Historical GitHub PR
metadata retains the archived original merge IDs. Current development checkouts
were not switched or rebased; user package-lock edits and Python cache remain
untouched. Start future development from the new main rather than merging an old
development branch into it. The user can re-enable Block force pushes in both
repos. No protections were changed by the agent; no deployment/game actions ran.
