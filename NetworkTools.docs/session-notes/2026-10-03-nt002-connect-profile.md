# NT-002 Connect elevation-profile experiment

Base: NT-023 655d685, branch dan/nt-002-connect-elevation-profile.

Pure station-based endpoint/join-grade construction passes 184 assertions. Endpoint choices and UI/provider controls are being integrated. Native CourseSplitSystem samples unowned new courses and can rewrite Y controls: authored math alone is not acceptance. Profile-off legacy generation remains unchanged. Optional profile requires independent native coverage and rejects a native mismatch, rather than dropping the constraint. Native verification pending.

Integrated production compilation and all 10 offline suites passed. Coverage helper: 190 assertions; profile: 184. Native mapping intentionally rejects unsupported monotone-chord violations and terrain-rewritten curves. Toy checkpoint CitiesIIAgentBridge-regression-before-reload-20261003-142116-2d5113df verified before graceful close. No deployment yet.

Full Debug/UI/postprocess/deployment passed at dc759cc. Native first profile-toggle preview remained unavailable because native temporary IDs were reused; no Apply. Clear/reselect isolated the next rejection: default hill-to-crest curve is nonmonotone, explicitly unsupported by conservative coverage. Adding an empty-preview boundary for guarded candidate replacements and a straight-control test variant; neither weakens native coverage.

Native 3611214: Simple and Complex straight controls accepted and applied with zero preview/permanent curve error, unchanged existing geometry, inherited Small Road prefab and connected graph. Different-height profile rejected CurveMismatch; current-token Apply rejected and original fingerprint unchanged. Default nonmonotone curve explicitly rejected. Native course sampling remains an unresolved feature limitation, not a passing terrain profile. Result checkpoints and exact binary hash retained in nt002-native-evidence.json.

Legacy profile-off curved Connect also passed native Apply, unchanged existing geometry, and zero preview/permanent error. Final Python run initially blocked build-identity because this shell lacked Git on PATH; refreshed process PATH and all 23 scripts passed. No tolerance weakened.
