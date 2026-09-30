# Optional NetworkTools automation provider

NetworkTools runs independently of the bridge. Debug builds additionally expose
`CitiesBridge.ProviderV1` with id `networktools`, protocol 1 and provider API 1.0.0.
There is no bridge DLL reference. Release currently omits the automation endpoint.
Bridge product versions and NetworkTools product versions are not lockstep; require
compatible provider protocol/commands and verify live state before automation.

Discover through list_providers, then invoke_provider with provider, descriptor
revision, command and args. Commands: state, activate, clear, select, strength,
split, apply. Their schemas are owned by ProviderV1.cs; state supplies current
NT session/revision/submission. Tool state and native preview guards are unchanged.
An accepted Apply is not permanent-result verification. Read-only state still
requires a loaded paused city under protocol 1's initial execution policy.

The runner translates its historical internal nt_* labels to generic calls locally;
the bridge contains no such aliases. Existing captures remain replayable through
their recorded interface; new live captures use generic envelopes. NT development
and release do not require bridge installation; bridge-assisted tests do.

Verification: non-deploying Debug compile and runner tests pass. Native provider
integration remains pending. This change does not add geometry behavior.
