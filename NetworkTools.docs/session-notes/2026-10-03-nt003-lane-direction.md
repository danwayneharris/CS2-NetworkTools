# NT-003 explicit Connect lane direction

Base PR20, commit6411865. Branch dan/nt-003-connect-lane-direction. Reusing explicit approach choice, live lane/composition identities and frozen candidate lifecycle. Lane mode default off; legacy behavior preserved. Lane diagrams select actual directed ordinary-road lanes. Native connection identity remains an independent acceptance check. Initial staged context/UI now integrated; production build and native tests pending.

## Offline checkpoint before native qualification

Implemented actual directed lane choices, contiguous compatible groups, endpoint handle constraints, shared UI/provider configuration and independent native direct-junction witnesses. Feature remains Debug-only and off by default. Physical slave lanes remain eligible; aggregate master lanes are excluded, following installed LaneSystem/NetCompositionSystem source rather than treating both as nonphysical.

Initial production compilation exposed ambiguous Edge/SubLane/CarLane imports; explicit Game.Net aliases corrected those errors. Non-deploying production compilation and the complete 12-suite offline aggregate now pass (including 42 direction assertions and 345 graph-proof assertions). Python runner corruption tests reject missing/reversed/wrong-lane results. These results do not establish native ECS mapping or UI behavior; full packaging and live qualification follow. Current graph checks prove direct selected-to-new-edge witnesses, not exclusive routing, graph stability across frames, vehicle use or end-to-end Complex path traversal.

## Native rejection and shared-port correction

Build fdbf10d packaged/deployed successfully. First query was before city readiness and correctly failed no_loaded_city; a second invocation lacked the required explicit fingerprint and made no mutation. Verified baseline run checkpointed successfully and rejected new_lane_mapping_unsupported; current-token Apply was blocked and permanent fingerprint unchanged.

Capture artifacts/nt003/lane-mapping-inspection revealed physical edge lanes with edge-owned middles but shared node-owned endpoint ports. Installed LaneReferencesSystem:163-176,237-307 collapses skipped junction lanes and rewrites those endpoints. Corrected the adapter to validate endpoint ownership against edge station and actual endpoint node, retain exact Temp.original/middle/composition/station interval, and independently prove directed connectivity through exact shared ports or explicit junction lanes. Never match only low lane bytes. The pure proof now has 402 passing assertions (57 additional wrong-owner/segment/station/secondary/duplicate and shared-port tests). Native retest remains pending for this correction.


## Native qualification of deb2994

Complete Debug/UI/postprocess/deployment passed. All 12 offline suites passed after the correction. Simple default controls and reversed Complex default controls both passed on independently restored terrain v1.1 baseline. Independent permanent lane graph proof found selected source -> new edge and new edge -> selected target; existing nodes and authored curves remained unchanged, expected Small Road prefab inherited, stale revision Apply rejected, and native preview/permanent matching was exact (0 m). Complex creates four native edges in this fixture; endpoint witnesses do not establish end-to-end vehicle use.

Simple result checkpoint: CitiesIIAgentBridge-regression-before-reload-20261003-151917-abe983f9. Review package artifacts/review/NT-003/NetworkTools. The subsequent reversed Complex result remains unsaved until the next lifecycle checkpoint. Added-lane highway/group/slave native cases, profile interaction, ordinary UI, reload equivalence, Release/Burst and traversal are not qualified. Retained as explicit draft-PR review gaps rather than treating two-way single-lane evidence as the whole plan.
