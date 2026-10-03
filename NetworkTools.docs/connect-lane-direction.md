# Connect lane-aware direction

Stage 5 / NT-003, source baseline `fdbf10d`, October 3, 2026. The Debug implementation and offline tests are present. Native preview/Apply acceptance and human review remain pending. This guide records implemented policy, not a claim that every supported-looking road has passed in-game verification.

## Features and usage

**Lane-aware direction** is an experimental, default-off, non-persistent Debug option for Simple and Complex Connect. Off retains the legacy direction behavior. Loop is unavailable. The feature chooses the connection's endpoint directions from explicit approach lanes; lateral lane alignment belongs to NT-022.

1. Select the two Connect endpoints and choose the incident approach edge at each endpoint using the approach controls.
2. Enable Lane-aware direction. At departure, select a lane travelling from the approach into the junction/new connection. At arrival, select a lane travelling from the new connection out into the approach.
3. Choose one eligible lane or an adjacent, same-direction group. Click a selected lane again to remove it, or use **Clear lane group**. A proposed incompatible group is rejected with an explanation.
4. Adjust the curve handles and wait for a current accepted native preview. The selected lane direction constrains each outer handle's horizontal axis; handle length remains adjustable. The candidate preparation preserves its horizontal handle length while placing it on that axis. Invalid/degenerate handles reject.
5. Apply only when the preview is accepted. An unavailable lane context or unverified native connection leaves Apply unavailable in lane-aware mode. Turning the feature off restores the legacy direction policy.

The diagram's frame is **looking from the junction outward along the selected approach**, with the junction depicted at the left of each row. Rows are ordered by lateral position in that frame. A left arrow points toward the junction; a right arrow points away. These are diagram travel arrows, not compass directions or the stored direction of the edge. The displayed lane index is a composition index, not a persistent lane entity identity.

Groups must be contiguous, within the same supported carriageway/group and travel role. Their horizontal directions must agree within one degree of the deterministic representative lane. The representative is the lower-middle member in lateral order; unrelated directions are never averaged. Ordinary road/highway car lanes are the initial scope. Two-way roads can expose eligible lanes in the required direction; this does not make a native shared `Twoway` lane supported. Physical slave lanes are allowed; aggregate master lanes are excluded.

Provider requests use the same domain policy: `laneAwareDirection`, `startApproach`, `endApproach`, `startLanes`, and `endLanes`. Entity choices include index and version. Context and status diagnostics expose missing, stale, incompatible, and native-rejected states. Do not cache a displayed lane index as a substitute for the current entity/context identity.

## Architecture

The feature separates source selection, pure direction policy, candidate preparation and native connection validation.

- [ConnectToolSystem.LaneContext.cs](../NetworkTools.Mod/Systems/Tools/Connect/ConnectToolSystem.LaneContext.cs) reads actual approach `SubLane` membership, `Owner`, `Lane` paths, `EdgeLane` endpoint stations, lane curves, prefab data and effective compositions. Matching requires an unambiguous composition index/prefab and consistent travel direction. It fingerprints endpoint/approach identities, composition and lane evidence. Changed evidence makes choices stale and requires reselection rather than silently substituting another lane.
- [LaneDirectionPolicy.cs](../NetworkTools.Mod/Systems/Tools/Connect/Core/LaneDirectionPolicy.cs) contains offline-testable endpoint orientation and group rules. Native lane curves already run along lane travel: incoming departure uses `d-c`; outgoing arrival uses `a-b`, because the new curve's end handle points against its arrival traversal. Endpoint stations and inversion are checked explicitly, including reversed stored edges. Group contiguity also checks physical road positions so an omitted intervening road lane cannot disappear from the policy merely because it is ineligible.
- [ConnectToolSystem.LaneDirection.cs](../NetworkTools.Mod/Systems/Tools/Connect/ConnectToolSystem.LaneDirection.cs) supplies effective endpoint axes and places the outer controls on them. Existing direction initialization remains the off-mode path. Lane selection changes direction, not endpoint positions or road width. With the optional smooth elevation profile, [ConnectToolSystem.Profile.cs](../NetworkTools.Mod/Systems/Tools/Connect/ConnectToolSystem.Profile.cs) uses the representative lane's measured grade, converting the arrival-handle grade to traversal orientation.
- [ConnectToolSystem.LaneValidation.cs](../NetworkTools.Mod/Systems/Tools/Connect/ConnectToolSystem.LaneValidation.cs) independently inspects the rebuilt temporary network. It finds unique temporary counterparts of selected nodes and approaches through `Temp.m_Original`, and a unique new edge at each endpoint. Each selected lane must map through its original identity, owner-normalized full path and exact composition/prefab evidence. Missing or ambiguous mapping rejects.
- [LaneConnectionProof.cs](../NetworkTools.Mod/Systems/Tools/Connect/Core/LaneConnectionProof.cs) proves a directed connection using full port identity: owner index, lane-and-segment value, curve station and secondary bit. The adapter independently checks full live entity identities and allowed lane types. At departure, **each selected incoming lane must have a direct junction NodeLane to some outgoing lane of the new edge**. At arrival, **each selected outgoing approach lane must have a direct junction NodeLane from some incoming lane of the new edge**. Duplicate evidence and wrong/missing ports reject; legitimate multiple outgoing witnesses are allowed.

This last condition is an existence proof at each endpoint. It does not designate a particular new-road lane group, reserve exclusive use, prove that both endpoint witnesses belong to one continuous route through every intermediate junction, or establish vehicle traversal. NT-022 must add explicit new-road correspondence rather than reuse this weaker proof as lateral-alignment acceptance.

Native-source basis is the installed Game 1.6.2f1 decompile: `Game.Net/LaneSystem.cs:4367-4368` and `4854-4872` map temporary edge lanes through owner-normalized original lane keys; `7280-7282` constructs directed source-to-target junction paths. `Game.Pathfind/PathNode.cs` defines exact key equality. Native physical lane endpoints may be set back from junction nodes; a lane station of 0 or 1 is not proof of spatial coincidence with a Node position.

The current preview acceptance and Apply gate re-read the connection proof. That checks current intent at acceptance/Apply; it is not a claim that the complete node-lane graph was included in the multi-frame stable-preview fingerprint. Geometry evidence and connection evidence remain distinct.

## Testing

Reported stage-5 offline result for this source baseline: **12 suites passed**, including **42 lane-direction policy assertions** and **345 directed-port proof assertions**. The direction suite covers endpoint orientation, travel roles, invalid source geometry and grouping/freshness policy. The proof suite exercises reversed arrows, missing witnesses, wrong owner/segment/station/secondary identity, duplicate evidence, legitimate one-to-many/many-to-one connections, ordering invariance, capacity/input failures, and explicit rejection of multi-hop-only evidence.

The tests execute production pure helpers; they do not simulate Unity ECS temporary-lane reconstruction. In particular, the physical-slave/native-master policy requires native verification beyond pure graph tests. Passing graph tests cannot establish that a live game's entity/composition adapter populated the graph correctly.

On clean Debug `deb29941bb2f`, Simple Connect and reversed Complex Connect passed native preview and permanent Apply: selected directed endpoint connections were independently observed, existing nodes/curves were unchanged, stale revision Apply was rejected, and preview/permanent control-point error was 0 m. See [compact evidence](session-notes/nt003-native-evidence.json). The initial shared-port adapter rejection and correction are retained in the session note. Reload equivalence, feature-off visual parity, added-lane highway and physical-slave group fixtures remain **not run**. Vehicle traversal and Dan's visual review are also **pending**. No native or traffic success is inferred from compilation, UI availability, or the offline suite totals.

## Limitations

- Debug opt-in only; Simple/Complex scope. Loop and arbitrary lane-connector editing are excluded.
- No lateral alignment, lane-width adjustment, exclusive routing, or promise that selected lanes are the only lanes connected.
- Shared/mixed rail or tram lanes, aggregate master lanes, native `Twoway` lanes, public-only/bicycle-only lanes, and other unsupported flags are rejected or excluded. Effective compositions that cannot be mapped exactly remain unsupported.
- A supported group supplies one representative direction and, when needed, grade. The horizontal agreement check does not imply that every member has identical vertical grade.
- The native proof supports exact shared node-owned ports and direct junction NodeLanes. Roundabouts, internal-node chains and other multi-hop-only routes can reject even if the game could route traffic through them.
- Rebuilt selected-lane identity, composition or segment changes can reject conservatively. Lane counts and approximate geometric matches do not substitute for identity.
- The proof checks each endpoint independently. Whole-route and vehicle-use qualification remain separate.
- Native reconstruction may reject a mathematically valid direction proposal. Anarchy is not evidence that lane direction or routing is correct.

## Dan's review

Use a disposable review save/checkpoint with an added-lane offramp/onramp and ordinary one-way and two-way approaches. Record the exact loaded build identity and save identity before comparing results. Use the stage-specific package `artifacts/review/NT-003/NetworkTools` (Debug `deb29941bb2f`) and the saved Simple result `CitiesIIAgentBridge-regression-before-reload-20261003-151917-abe983f9`. Native endpoint/geometry evidence exists for two-way Small Road; your visual/traffic review remains pending.

1. **Legacy baseline:** leave Lane-aware direction off. Capture the existing degree-two perpendicular departure behavior and the original neighboring roads.
2. **Explicit departure:** choose the intended approach and outer incoming lane, then an eligible outgoing arrival lane. Confirm the diagram's outward-looking frame and arrows against actual traffic direction. Check that an opposite-role lane is unavailable.
3. **Group behavior:** select an adjacent compatible group. Try a gap, conflicting direction/group, and a changed approach. Expect a clear rejection or stale-choice request, never an automatically chosen replacement.
4. **Handle constraint:** change outer handle length and inspect preview. Expect the chosen horizontal direction to remain constrained. Compare Simple and Complex; separately exercise smooth elevation profile on/off and sloped approaches.
5. **Reversal:** reverse endpoint selection in a suitable fixture, selecting lanes appropriate to the reversed travel role. Confirm that stored edge orientation does not reverse the intended arrows or handles accidentally.
6. **Native acceptance and Apply:** inspect selected lane identities and directed junction connections, then compare permanent output and reload. Check node positions, neighboring authored curves, junction shape and markings. An accepted status means each selected lane has the documented endpoint witness, not exclusive routing.
7. **Traffic review:** observe vehicles entering and leaving the ramp separately. Record actual vehicle use, wrong/extra connections and any unexpected routing. Do not mark traffic qualified solely because preview/Apply or the provider reports acceptance.

Expected outcome: explicit lane choices produce understandable endpoint direction constraints; unsupported or stale contexts are visible; default-off behavior remains available. Record failures and limitations as well as successful fixtures before declaring native acceptance complete.

Source-grounded correction: native `LaneReferencesSystem` can collapse a skipped degree-two junction lane to a shared node-owned port. Exact shared port equality is valid directed continuity only after the adapter establishes the distinct actual owning edges and incoming/outgoing roles. Endpoints are not assumed to retain composition lane bytes; middle identity, exact `Temp.original`, composition and edge interval are retained.
