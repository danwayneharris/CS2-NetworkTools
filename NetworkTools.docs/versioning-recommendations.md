# Versioning audit and deferred recommendations

Audited 2026-09-28. This is a housekeeping proposal, not an implemented versioning
change. No build, deployment, version bump, or publishing was performed.

## Finding

**Versioning exists and uses the installed toolchain's publishing mechanism.**
The gap is identifying our development builds: distinct fork builds still identify
themselves as upstream `1.5.7`, without their source revision. The UI bundle also
carries a separate, stale `1.0.0` label. This is inconsistent diagnostic identity,
not evidence that the SDK rejects the mod.

## What exists

| Layer | Current value and source | Meaning |
| --- | --- | --- |
| Project version | `1.5.7`, [NetworkTools.csproj](../NetworkTools.Mod/NetworkTools.csproj#L11) | Source for the release number. |
| Assembly / file version | `$(Version).0`, same project, lines 12–13 | Numeric `1.5.7.0`. Generated Debug and Release AssemblyInfo files confirm this. |
| Informational version | Generated value `1.5.7` | [Common props](../NetworkTools.Mod/Common/LucaModsCommon.props#L21) explicitly disables automatic source-revision inclusion. |
| Publisher version | Generated XML currently `1.5.7` | [Common targets](../NetworkTools.Mod/Common/LucaModsCommon.targets#L42) copies the publish template and substitutes `$(Version)`, long description, and `Properties/Changelog/<version>.md`. This runs on the publishing path, not every ordinary build. |
| UI bundle label | `1.0.0`, [mod.json](../NetworkTools.Mod/UI/mod.json#L4) | [webpack](../NetworkTools.Mod/UI/webpack.config.js#L17) uses it in the bundle banner. It is not the publisher's ModVersion. |
| npm package | No root package version in [package.json](../NetworkTools.Mod/UI/package.json) | The frontend is not currently versioned as a separately published npm package. Dependency package versions are a different concern. |

The [publish template](../NetworkTools.Mod/Properties/PublishConfiguration.template.xml#L22)
still contains `ModVersion=1.1.10`, but the generation target overwrites that field.
The template's `GameVersion=1.6.*` differs from the checked-in generated XML's
`1.5.*`; the generated file is therefore not a reliable view of the next publish
until regenerated. GameVersion describes claimed game compatibility, not the mod
release number, and should reflect verified support.

There is **already an About-section UI** exposing both Version and Informational
Version: [Settings.cs](../NetworkTools.Mod/Settings/Settings.cs#L149). They read assembly
metadata through [LucaModBase.cs](../NetworkTools.Mod/Common/Mod/LucaModBase.cs#L170).
The same base class logs the numeric assembly version at startup (line 199).
We need better values and diagnostic reporting, not a new permanent toolbar label.

## Installed toolchain and loader evidence

The project imports installed `Mod.props` and `Mod.targets`, then Common targets.
The installed `.cache/Modding/Mod.targets`, lines 116–155, resolves the publish XML
and passes it to ModPublisher; it does not impose a per-commit version scheme.
The existing [NewVersion profile](../NetworkTools.Mod/Properties/PublishProfiles/PublishNewVersion.pubxml)
selects Release and `ModPublisherCommand=NewVersion`.

The source snapshot recorded in `../cs2-decompile/source-manifest.json` identifies
game `1.6.2f1 (767.21d1) [6300.26419]`. In that local snapshot,
`src/Colossal.IO.AssetDatabase/Colossal.IO.AssetDatabase/ExecutableAsset.cs`:

- Line 149 obtains executable version from the assembly definition.
- Lines 181–189 choose among same-name executable assets by already loaded first,
  local next, numeric version next, then asset ID.

Thus assembly version is not purely cosmetic. It participates in duplicate
selection; a higher version is not a hot-reload mechanism or a safe way to make
two forks coexist. Preserve the assembly/UI ID alignment for now. Decide naming
and coexistence explicitly before distributing a separate fork.

These findings come from the local project, installed targets, generated assembly
metadata, and versioned decompile, not a claim that we exercised a publication.
Publisher acceptance of proposed prerelease strings has not been tested.

## Recommended policy

| Event | Recommendation |
| --- | --- |
| Every local build | Record source commit, dirty state, configuration, and a binary hash in diagnostics/build manifest. Do not edit a tracked release number each build. |
| Ordinary commit or PR | Keep the planned release number stable; describe behavior and validation in the PR/session notes. A PR number is not a binary identity. |
| Test artifact handed to someone | Give it a recognizable fork/build identity and immutable artifact hash; identify whether the source was dirty. |
| Release candidate / release | Bump the release number deliberately, update its changelog and tested GameVersion range, synchronize metadata, validate, then tag the exact release commit. |

Use one release-version source and derive numeric assembly/file versions from it.
Keep prerelease labels and commit IDs in informational metadata, not in
`AssemblyVersion=$(Version).0`: inserting a string such as `1.6.0-dev` into the
current Version property would make that numeric expression invalid. If we adopt
prerelease Version values, separate the numeric base version first.

For example, a *hypothetical* informational value might be
`1.6.0-dan.dev+gabcdef123456.dirty` while numeric assembly/file versions remain
`1.6.0.0`. This is an example, not a selected next release or an assertion about
Paradox Mods prerelease support. Set the final public numbering/branding when we
decide whether this is an upstream contribution or a separately distributed fork.

Implement the development identity in the parent project/build tooling, overriding
Common props after import as needed; do not advance or edit the pinned Common
submodule incidentally. Merely re-enabling SourceRevisionId inclusion would still
leave dirty-tree detection, configuration, and deployed binary identity unresolved.
Use `unknown` explicitly when Git metadata is unavailable rather than inventing an ID.

Expose that identity in the existing About fields and startup log, and include it
in junction captures. A small optional Debug-panel build label or copy-diagnostics
button would help screenshots, but ordinary players do not need it in the main
tool panel. Generate the UI version from the same source or validate equality
during packaging; also record the UI bundle hash so an old UI/new DLL pairing is
detectable. A DLL hash identifies an artifact more precisely than a commit alone.

## Before any fork publication

Both publish template and generated XML still carry upstream `ModId=133736`,
upstream branding and links. A GitHub PR to our fork is unrelated to a Paradox Mods
publication. Do not run the existing publisher configuration as though it were
our fork's listing. Prepare separate owned listing metadata (new ID obtained on
first publication), release notes, compatibility claims and identity policy first.
No publishing or ownership changes are part of this audit.

## Suggested follow-up acceptance checks

1. Build two different commits and confirm different informational identities;
   make an uncommitted code change and verify the dirty marker.
2. Check generated assembly attributes, About values and startup diagnostics agree.
3. Check generated publisher ModVersion, changelog selection and UI label agree
   with the intended release without invoking the publisher.
4. Verify missing Git metadata is handled honestly and no build changes tracked
   source solely to stamp a version.
5. On the next supervised deployment, compare the captured identity and artifact
   hashes with the files actually deployed; a successful compile does not prove
   which assembly is loaded in a running game.
