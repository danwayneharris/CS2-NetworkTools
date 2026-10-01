# Native audit gate recipes

These scripts mutate only explicitly verified, paused toy saves. Start with the checksummed terrain-v1.1 fixture and a working generic bridge. `create-two-ended-incident-fixture.py --straight --run` creates/checkpoints an alternate Medium Road; inspect the native result before doing anything else. The curved variant deliberately retains the failed two-edge experiment.

The straight branch may be shorter than the original path. Its prefab transition penalty is additive (9.9 per change), not multiplicative. The captured `lengthen-fixture-example.cs.txt` uses unity-devtools eval to bow the existing single edge without adding a node. IDs in these example recipes belong to one recorded session: resolve and validate fresh entities before adapting them. Read the unity-driving skill first; verify bridge STOP/control settings and a recoverable checkpoint before writes. Save a new baseline and record its hash/fingerprint after native rebuilding.

`prepare-native-slope-gate.py --run` validates a fixture/hash and selects/configures a native Slope preview without Apply. Read `m_CurrentPathEdges` from the live tool with unity-devtools: bridge BFS is not NT weighted route selection on cyclic graphs. `finish-native-slope-gate.py --run --selected-edges index:version,...` requires that separately captured witness, validates graph continuity and current input/token/city identity, Applies once, and independently checks permanent topology, lanes, both incident translations and preview agreement. Run `audit-slope-capture.py` on its output as an additional independent check. Never reuse a prepared capture after reload.

`stale-apply-example.cs.txt` changes an original curve within one debugger suspension, calls the shared domain Apply check, and restores the curve before the frame resumes. Successful observation is allowed-before, rejected-while-changed, rejected-request, allowed-after-restoration and unchanged Ready phase. It proves the original-value guard, not arbitrary concurrent-mod races or ordinary UI coverage. If eval fails partway, inspect current native state before restoration/retry.


`capture-native-shape-gate.py --phase before|after` is a read-only Release oracle.
Before requires a checksummed fixture and exact fingerprint, then captures native
preview. Invoke the existing selection handlers and shared TryRequestApply
separately, then run after in the same city session. This narrow helper assumes
the fixture's simple nonjunction tree path; it is not a weighted route oracle for
cyclic graphs. It checks preview/permanent agreement, topology, lane mappings,
fixed endpoints, unselected geometry and the operation's preserved axes. Neither
phase requests Apply. Raw outputs belong under ignored artifacts directories.
