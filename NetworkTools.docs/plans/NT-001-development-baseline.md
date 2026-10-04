# NT-001: trustworthy development baseline

Status: reviewed planning draft; saved October 3, 2026. Not yet executing.
Order: 1 of 6. Baseline: merged main f36d669 plus the roadmap-consolidation documentation.
Dependency: none within this stack. Verify the current merged baseline before execution.
Proposed branch: dan/nt-001-development-baseline.
All [shared execution rules](first-six-execution-contract.md) and [documentation requirements](README.md#required-implementation-document-all-plans) apply.

## Intended result and scope

Make the running build identifiable, the existing verification suites discoverable,
and current documentation trustworthy before further feature development. This is
bounded housekeeping, not release preparation, rebranding, or a test-framework rewrite.

## Implementation

1. Reconcile README, AGENTS, bootstrap/build instructions, architecture documentation,
   and relevant feature guides against current source and later evidence. Mark
   superseded research with successor links without erasing historical findings.
2. Keep the numeric release version unchanged. Add source revision, dirty state,
   and build configuration to development informational metadata. Reuse the existing
   About display, startup diagnostics, and NT provider status.
3. Produce a post-build manifest containing binary hashes. Do not embed a binary's
   own final hash inside it. Derive the UI version information from the same
   release/build metadata source.
4. Preserve assembly identity, publishing conventions, and the pinned Common
   submodule. Override metadata in the parent build rather than modifying Common.
5. Reuse the existing non-deploying offline aggregate. Report missing prerequisites,
   empty coverage, unavailable native fixtures, and actual failures distinctly.
6. Include the existing committed roadmap/planning work in this first PR if it
   remains unpublished; do not silently lose it when creating sprint branches.

Read [versioning recommendations](../versioning-recommendations.md) and reconcile
them with current source. Source archives without Git must report an unknown
revision honestly rather than inventing a clean identity.

## Interfaces and documentation

Additive development identity fields may be exposed through the existing NT
provider; the Bridge remains generic. No numeric version bump per commit.
Create or update a durable development-build guide covering usage, architecture,
testing, limitations, and **Dan's review**. Record command deployment side effects.

## Verification and acceptance

- Exercise clean, dirty, and no-Git source metadata generation.
- Check consistency across assembly metadata, UI/About, provider, and manifest.
- Run the existing offline aggregate and a non-deploying build.
- Where live access is available, compare running identity with the deployed
  artifact hash. Report unavailable native identity verification explicitly.
- Do not repeat every historical game experiment solely for housekeeping.

Success means truthful identity and reproducible existing checks, not a release
qualification claim. Unexpectedly empty suites must not pass.

## Dan's review

The completed guide must supply exact revisions/configuration and any deployed hash.

1. Open About and compare identity with the startup log and review manifest.
2. Follow the documented non-deploying test command and inspect its summary.
3. Review current versus experimental claims and the roadmap links.

Expected: consistent identity, understandable commands, and no feature/default change.
No new visual geometry or vehicle-traversal claim is made by this stage.

## Delivery

Incremental session notes and local commits; one draft PR targeting the verified
main baseline. Link the durable guide and test summary. Retain a reproducible build
checkpoint for independent later review.
