# NT-002 Connect elevation-profile experiment

Base: NT-023 655d685, branch dan/nt-002-connect-elevation-profile.

Pure station-based endpoint/join-grade construction passes 184 assertions. Endpoint choices and UI/provider controls are being integrated. Native CourseSplitSystem samples unowned new courses and can rewrite Y controls: authored math alone is not acceptance. Profile-off legacy generation remains unchanged. Optional profile requires independent native coverage and rejects a native mismatch, rather than dropping the constraint. Native verification pending.

Integrated production compilation and all 10 offline suites passed. Coverage helper: 190 assertions; profile: 184. Native mapping intentionally rejects unsupported monotone-chord violations and terrain-rewritten curves. Toy checkpoint CitiesIIAgentBridge-regression-before-reload-20261003-142116-2d5113df verified before graceful close. No deployment yet.
