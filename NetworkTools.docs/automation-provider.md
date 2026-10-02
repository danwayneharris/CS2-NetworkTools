# Optional NetworkTools automation provider

NetworkTools runs independently of the bridge. Debug builds additionally expose
`CitiesBridge.ProviderV1` with id `networktools`, protocol 1 and provider API 1.1.0.
There is no bridge DLL reference. Release currently omits the automation endpoint.
Bridge product versions and NetworkTools product versions are not lockstep; require
compatible provider protocol/commands and verify live state before automation.

Discover through list_providers, then invoke_provider with provider, descriptor
revision, command and args. Commands: state, activate, clear, select, strength,
split, apply. Their schemas are owned by ProviderV1.cs; state supplies current
NT session/revision/submission. State includes `smoothMode`; activation is ready only when both `active` and
`smoothMode` are true. Native preview guards remain enforced.
An accepted Apply is not permanent-result verification. Read-only state still
requires a loaded paused city under protocol 1's initial execution policy.

The runner translates its historical internal nt_* labels to generic calls locally;
the bridge contains no such aliases. Existing captures remain replayable through
their recorded interface; new live captures use generic envelopes. NT development
and release do not require bridge installation; bridge-assisted tests do.

Verification: full Debug build/deployment and eight runner tests pass. On
2026-09-30 the generic provider completed the rail-merge fixture at strengths
0.5 and 0.8, including independent permanent-result checks after Apply. Captured
incident curves matched exactly; directed connections and fixed nodes were preserved.
Vehicle traversal and new human visual validation remain unverified.
This change does not add geometry behavior.

## Slope and Connect commands (provider 1.1.0, Debug only)

The original seven Curve commands retain their names. Two explicit command groups
add `slope_` and `connect_` prefixes to `state`, `activate`, `clear`, `select`,
`configure`, and `apply`. Discover the descriptor revision afresh after deployment.
All changes except activation require the tool's own `session` and `revision`;
Apply additionally requires the observed `submission`. Activation clears an
existing selection and chooses the group's default mode. No bridge code changes
or NetworkTools dependency in the bridge are required.

Slope activation selects linear mode. `slope_configure` requires a complete set:

```json
{"session":"from slope_state","revision":42,"mode":"ease",
 "easeIn":0.1,"easeOut":0.1,"archHeight":10,"archPosition":0.5,
 "smoothStart":true,"smoothEnd":true}
```

Modes: `linear`, `ease`, `arch`. Ease parameters are 0..0.5, arch height -80..80 m,
arch position 0.1..0.9. All fields are explicit so persisted UI settings cannot
silently choose an experiment's configuration. Select existing path endpoints
using `{index,version}` identities. Slope deliberately changes heights; Curve's
unchanged-elevation assertion is therefore not a valid Slope oracle.

Slope readiness checks the current original inputs and native edge curves against
the job output. A mismatch stays unverified and blocks automated Apply. On the
terrain off-ramp, two of seven requested curves currently differ from the native
preview. This is a recorded integration limitation, not resolved by exposing the
API. The ordinary hill-road ease trial passed exact preview/permanent comparison.
Linear and arch configuration are implemented but not yet verified through Apply.

Connect initially supports `SimpleCurve` between existing degree-one nodes with
matching node prefabs, at most 2 km apart. Activation resets prefab override and
uses the endpoint's network type. Complex curves, loops, arbitrary prefab choice,
new free endpoints and junction endpoint selection are not exposed yet.
`connect_configure` accepts `startControl` and `endControl` as `[x,y,z]`, plus the
current session/revision. Coordinates must be finite and within 4 km of the start.
Endpoint positions remain derived from selection. The built-in Anarchy setting is
reported, not silently changed by automation.

Connect watches the actual newly rebuilt native temporary edges after its own
submission, requires lane buffers and three repeated stable observations, and
rechecks the exact snapshot at Apply. Previous-preview entity reuse remains
unverified. These observations are bounded evidence, not a universal native
completion fence or lane/collision certification. Clients must inspect the
resulting permanent network independently. State exposes a diagnostic preview
snapshot, whose internal array representation is not a stable general geometry API.

Live October 1 verification: full Debug build/postprocess/UI/deploy; original
Curve off-ramp regression passes; Slope ease on hill road passes with identical
preview/Apply curves, unchanged topology and no unselected curve changes; Connect
creates two permanent road segments and one node, matching native preview curves
exactly. Both new groups reject stale revisions. The tested Connect run had
Anarchy enabled. Release compilation passes with endpoints omitted; this is not
Release/Burst postprocessing or game verification. Eight existing runner tests pass.

Reusable `scripts/exercise-tool-provider.py` requires `--run`, `--stage`, a save
root and fresh output directory. Curve starts from the checksummed v1.1 fixture;
Slope/Connect additionally require an explicit known `--expected-fingerprint`.
Every stage verifies a recoverable checkpoint before mutations. Captures under
`session-notes/captures/provider-tools-*` include the blocked ramp experiment.

### Combined experiment (provider 1.2.0, Debug only)

After ordinary `activate`, call `combined` with the current `session`, `revision`
and `enabled: true`. Activate intentionally resets this option off for backwards
compatibility. Strength continues to control horizontal smoothing. `state` adds
`combinedSlope`, `surfaceAccepted`, and `surfaceFailed`; use `previewReady` and the
current submission for Apply, not a single stage's acceptance flag. The command
changes a preview parameter, not permanent geometry. Apply remains an explicit,
separately validated mutation. This interface is under integration; see the
combined architecture session note for verification status.

The combined command also accepts optional boolean `smoothStart` and `smoothEnd`,
matching the visible boundary controls. Regression fixtures set them explicitly;
otherwise the existing tool values are retained. `rejectionReason` is a localization
key shared with the manual Apply panel. These controls remain Debug-only.
