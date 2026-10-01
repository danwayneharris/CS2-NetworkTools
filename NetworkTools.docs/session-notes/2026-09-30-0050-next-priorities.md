# Saved priorities and bridge coupling review — 2026-09-30 00:50

Saved the previous planning response in ../next-development-priorities.md and added
a separate proposed provider-contract step in response to Dan's coupling concern.
Read the cs2-modding skill and its mod-compatibility reference, the MCP feasibility
report, bridge adapter/dispatch/catalog/client source, and NT's actual InvokeV1
implementation. A guessed Automation/BridgeApi.cs path did not exist; rg located
the endpoint in RoadShapeToolSystem.Automation.cs instead.

Findings: reflection removes the direct assembly reference, not the hardcoded
NetworkTools identity or duplicated command inventory. Recommend generic opt-in
providers with mod-owned handlers/descriptors and bridge-owned transport/policy,
then derive MCP tools from that contract. Preserve established game-thread, session,
revision and outcome rules. Proposed work only; no runtime or game changes.

Created dan/next-sprint-plan from rewritten main in the separate nt-history-review
worktree. User package-lock edits and original development checkout are untouched.
Documentation-only verification: reviewed saved plan and source references; no build
or live test necessary and none claimed. Commit locally; no PR/push requested.
