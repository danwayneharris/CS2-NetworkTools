# Versioning audit — 2026-09-28 10:45

Requested as an independent sub-agent investigation while junction work continues.
Read repository setup/build/architecture guidance and the cs2-mod-project skill,
then inspected project properties, Common imports, generated assembly attributes,
UI metadata, settings, installed SDK targets and the manifest-backed game loader
source. The working tree was clean before writing these two documents.

Added [deferred recommendations](../versioning-recommendations.md). Existing
versioning follows the SDK publishing route and already has About UI fields.
Development build identity is missing: all builds report upstream 1.5.7, source
revision inclusion is disabled, and the UI banner says 1.0.0. The publish template
does generate ModVersion from the project, but checked-in generated compatibility
metadata can be stale. Upstream publication identity requires deliberate treatment
before independently distributing this fork.

Recommendation: stamp diagnostic build identity per build; bump release versions
at release preparation rather than every commit or PR. Keep numeric assembly
versions separate from any prerelease/commit label. No code or version changes,
builds, deployment, publication, or in-game tests performed. Root agent will review
and commit the documentation; this sub-agent made no Git commits.
