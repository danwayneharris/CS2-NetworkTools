# Development build identity (NT-001)

Status: implementation in progress; verification below is updated as checks finish.
This is development housekeeping, not a release version bump or Release qualification.

## Features and usage

Numeric version remains 1.5.7 / 1.5.7.0. Informational version records the source
revision, clean/dirty/unknown working-tree state, and configuration. About already
shows informational version. Startup logs now report it; every NT provider result
adds build.assemblyVersion, build.informationalVersion, and build.configuration,
read from the loaded assembly rather than the current mutable checkout.

Run bootstrap.ps1 -Build -PackageOnly for full postprocessing/UI output under
artifacts/packages/Debug without overwriting the installed mod. Ordinary -Build
still deploys and requires CS2 closed. -OfflineTest does not deploy or contact CS2.
-PackageOnly requires -Build and cannot be combined with legacy -Test.

## Architecture

The parent project imports BuildIdentity.targets without changing Common. Metadata
is captured before assembly attributes are generated, then supplied to the UI build
so its banner uses the same identity. A completed-package manifest hashes final
postprocessed DLL/UI/assets; it excludes itself. NTDeploy=false redirects the SDK
copy target and UI output into the isolated package directory.

Direct standalone npm builds must receive NT_BUILD_IDENTITY from a corresponding
metadata capture; prefer the project build to avoid a stale/unrelated banner.
Build configuration and diagnostic identity are not proof of native execution.
No assembly identity, publisher version, or bridge protocol dependency changed.

## Testing

Agent checks: four build-identity fixture tests passed, covering clean/dirty/revision,
no-Git metadata, artifact hashes/incomplete package, and UI metadata. Installed SDK
assembly-attribute generation and the package-directory redirection check passed.
Full Debug package (compile, postprocess, UI, final manifest) passed. The seven-suite offline aggregate passed, including 22 Python scripts; five research CLIs remain explicitly not run. No new live verification yet. Reports: artifacts/nt001/package.log and artifacts/nt001/offline/summary.json.

## Limitations

Missing Git reports unknown rather than claiming clean provenance. Dirty identity
is not a content hash of the entire source tree; final artifact hashes distinguish
outputs. Current evidence does not establish Release/Burst, vehicle traversal, or
new human visuals. The full Debug package path passed; Release packaging is not newly qualified.

## Dan's review

Stage-specific revision/artifact identity will be recorded after verification.
No new toy save or checkpoint has been created for this housekeeping stage.

1. Build this PR with -Build -PackageOnly and inspect its manifest.
2. After closing CS2, deliberately deploy the same stage with -Build.
3. Inspect About and the startup log; compare with the package identity.
4. In a paused toy save, query NT provider state and compare its build object.
5. Run -OfflineTest; inspect failures, blocked prerequisites, and not-run research.

Expected: consistent source/build identity and honest test status, with unchanged
geometry behavior. This stage does not claim new visual or traffic qualification.