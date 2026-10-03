# Combined interior junction elevation follow-up

Dan reported that combined Constant Slope preserves unwanted interior junction heights. Read-only live selection: seven edges, two three-way interior junctions, combined enabled, ready, strength 1. Source confirms full XYZ and separate original grades were pinned.

Authorized policy adjustment: keep interior junction XZ, release Y and selected-path vertical grades; preserve outer endpoints and explicit split XYZ. Reuse existing unique incident-edge endpoint/adjacent-handle translations for side branches, with both endpoint edits composed. Validate predicted side-road changes rather than disabling outside-selection checks. Native directed connections remain mandatory.

Initial implementation adds a production-fit counterexample with a junction ten metres above the desired grade. Native testing and deployment pending; do not claim success from compilation.

Offline implementation verified: all seven aggregate suites passed, including 34 combined production-transform assertions and 11 live-runner unit tests. Incident oracle independently computes per-end Y translation and rejects missing translations, XZ edits, altered handles and nonfinite controls. Initial compile caught an observer parameter-name mistake; corrected before passing tests. PowerShell stderr handling stopped the first aggregate wrapper on a harmless uv warning; direct invocation of the same aggregate passed. No test was relabeled.

Captured baseline: `CitiesIIAgentBridge-combined-interior-junction-baseline-20261002-040829-f049c06c.cok`, 40 nodes/41 edges in bounded region; fixture records package SHA and geometry fingerprint. Native deployment pending.

Native attempt: Debug full build/postprocess/UI/deploy passed (32 warnings, 0 errors), DLL SHA256 `6EC3B8AAF922CC332898ED89ADB1762CAAF0DB2BB91057AEE2E9885D0383F360`. Preserved another checkpoint before graceful restart. First combined preview accepted and Apply completed. The runner stopped first on a capture-boundary oracle bug (an unrelated edge extends outside the node-query area). Corrected the oracle to require sampled displacement only for selected affected endpoints and still check every unselected control. Added a replay script to re-evaluate all captured assertions without new mutation.

Offline replay then exposed a genuine physical lane transition difference at a degree-two node: edge 60110 lane 3 -> edge 60109 lane 3 became edge 60110 lane 2 -> edge 60109 lane 3. The two degree-three interior junctions retain their directed transitions. Do not claim this run passed. Repeated mutation stopped; curve-only baseline comparison launched to isolate whether vertical changes caused the difference. Production applied-state repeat unit tests now pass 40 combined assertions.

Curve-only control from the identical original baseline produced the same lane-3-to-lane-2 change at (-1677.42542, 620.326233, -2348.58936), plus another lane change at (-1893.42688, 614.6611, -2318.354). Thus the first transition is reproducible without combined vertical fitting; this is evidence of pre-existing horizontal/native lane remapping, not proof that every combined case preserves all lanes. Keep the first-Apply failure visible and defer broader degree-two lane-preservation work. No invariants were removed to obtain a pass.

The captured user's junction elevations were already almost on the overall grade: one changed only 1.3mm (trivial under the agreed tolerance). The more meaningful change in this example is releasing the original vertical handle grades at the two junctions. The deliberately displaced offline fixture verifies actual height freedom separately.

Final aggregate after checker/test changes: all seven suites passed. Prepared a clearly labeled already-applied baseline for further repeatability checks; it must not be confused with the original-baseline first-Apply test.

Repeatability: loaded the saved first combined result and ran two additional combined Applies. Both passed all six watched-node directed/physical lane checks, intended incident geometry, fixed/XZ constraints and exact preview/permanent curve agreement. Between the two repeats, maximum node displacement was zero and maximum control displacement 0.00012m (0.12mm, trivial). The original-baseline lane failure remains recorded separately.

Final review checkpoint: `CitiesIIAgentBridge-junction-height-corrected-review-20261002-042335-70f78312.cok`. Game paused; seven-edge selection active at strength 1.0, Constant Slope enabled, Smooth Start/End enabled, preview ready. No unsaved permanent edits after checkpoint; only re-selection. Runtime 00c789a/DLL hash above unchanged. Later commit contains offline tests, oracle capture-boundary fix, replay helper and documentation only. No human visual/vehicle verification claimed.
