# NT-003 explicit Connect lane direction

Base PR20, commit6411865. Branch dan/nt-003-connect-lane-direction. Reusing explicit approach choice, live lane/composition identities and frozen candidate lifecycle. Lane mode default off; legacy behavior preserved. Lane diagrams select actual directed ordinary-road lanes. Native connection identity remains an independent acceptance check. Initial staged context/UI now integrated; production build and native tests pending.

## Offline checkpoint before native qualification

Implemented actual directed lane choices, contiguous compatible groups, endpoint handle constraints, shared UI/provider configuration and independent native direct-junction witnesses. Feature remains Debug-only and off by default. Physical slave lanes remain eligible; aggregate master lanes are excluded, following installed LaneSystem/NetCompositionSystem source rather than treating both as nonphysical.

Initial production compilation exposed ambiguous Edge/SubLane/CarLane imports; explicit Game.Net aliases corrected those errors. Non-deploying production compilation and the complete 12-suite offline aggregate now pass (including 42 direction assertions and 345 graph-proof assertions). Python runner corruption tests reject missing/reversed/wrong-lane results. These results do not establish native ECS mapping or UI behavior; full packaging and live qualification follow. Current graph checks prove direct selected-to-new-edge witnesses, not exclusive routing, graph stability across frames, vehicle use or end-to-end Complex path traversal.
