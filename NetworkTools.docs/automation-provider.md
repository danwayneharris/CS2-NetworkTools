# Optional NetworkTools automation provider

NetworkTools runs independently of the bridge. Debug builds additionally expose
`CitiesBridge.ProviderV1` with id `networktools`, protocol 1 and provider API 1.0.0.
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
