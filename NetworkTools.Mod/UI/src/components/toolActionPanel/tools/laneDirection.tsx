import React from "react";
import { laneLabel, laneOrdinal } from "./laneLabels";
import { useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import { ConnectMode, PARAM_BINDINGS } from "generated/parameters.generated";
import { GAME_BINDINGS, GAME_TRIGGERS } from "gameBindings";
import { ParameterField } from "../shared/parameterField";
import styles from "../toolActionPanel.module.scss";

type LaneEntity = { index: number; version: number };
type LaneChoice = { lane: LaneEntity; index: number; position: number; carriageway: number; incoming: boolean; selected: boolean; eligible: boolean; reason: string | null };
type LaneEndpoint = { endpoint: number; node: LaneEntity; approach: LaneEntity; choices: LaneChoice[]; reason: string | null; choiceError: string | null };
const sameLane = (a: LaneEntity, b: LaneEntity) => a.index === b.index && a.version === b.version;

const reasonText = (reason: string): [string, string] => {
    if (reason === "lane_choice_required") return ["LaneChooseShort", "Select a lane."];
    if (reason === "lane_wrong_travel_role") return ["LaneWrongRoleShort", "Opposite traffic direction"];
    if (reason === "lane_type_unsupported") return ["LaneUnsupported", "This lane type is not supported by lane-aware direction."];
    if (reason === "lane_disconnected") return ["LaneDisconnected", "This lane is disconnected at the selected endpoint."];
    if (reason === "lane_choice_stale") return ["LaneStale", "The approach or lanes changed. Choose the lanes again."];
    if (reason === "lane_group_not_contiguous") return ["LaneContiguous", "Choose adjacent lanes without a gap."];
    if (reason === "lane_group_direction_conflict" || reason === "lane_group_incompatible") return ["LaneConflict", "The selected lanes do not share a supported direction and group."];
    if (reason === "lane_context_pending") return ["LanePending", "Waiting for the road's lane rebuild to finish."];
    if (reason.startsWith("profile_")) return ["LaneApproachShort", "Choose the road segment above first."];
    return ["LaneUnavailable", "Lane geometry or composition could not be verified. This context is unavailable."];
};

export const LaneDirectionControls: React.FC = () => {
    const available = useValue(GAME_BINDINGS.CONNECT_LANE_DIRECTION_AVAILABLE.binding);
    const mode = useValue(PARAM_BINDINGS.connect.mode.binding) as ConnectMode;
    const enabled = useValue(PARAM_BINDINGS.connect.laneAwareDirection.binding);
    const status = useValue(GAME_BINDINGS.CONNECT_LANE_DIRECTION_STATUS.binding);
    const json = useValue(GAME_BINDINGS.CONNECT_LANE_DIRECTION_CHOICES.binding);
    const endpoints: LaneEndpoint[] = React.useMemo(() => JSON.parse(json), [json]);
    const { translate } = useLocalization();
    const label = (key: string, fallback: string) => translate(`NetworkTools.UI.Connect.${key}`, fallback) ?? fallback;
    const reasonLabel = (reason: string) => { const [key, fallback] = reasonText(reason); return label(key, fallback); };
    const send = (endpoint: LaneEndpoint, lanes: LaneEntity[]) => GAME_TRIGGERS.SET_CONNECT_LANE_DIRECTION(JSON.stringify({
        endpoint: endpoint.endpoint, node: endpoint.node, edge: endpoint.approach, lanes
    }));
    if (!available) return null;
    if (mode === ConnectMode.Loop) return <div>{label("LaneLoop", "Lane-aware direction is unavailable for Loop.")}</div>;
    return <>
        <ParameterField paramKey="connect.laneAwareDirection" />
        {enabled && <div className={styles.helper}>{label("LaneOrientation", "Left/right follows traffic. Choose one lane or adjacent lanes.")}</div>}
        {enabled && status.startsWith("lane_native_") && <div>{label("LaneCheckFailed", "Lane connection not verified. Adjust the curve.")}</div>}
        {enabled && endpoints.map(endpoint => <div className={styles.splitChoices} key={endpoint.endpoint}>
            <div>{endpoint.endpoint === 0
                ? label("LaneDepartureShort", "From: incoming lanes")
                : label("LaneArrivalShort", "To: outgoing lanes")}</div>
            {endpoint.reason && <div>{reasonLabel(endpoint.reason)}</div>}
            {endpoint.choiceError && <div>{reasonLabel(endpoint.choiceError)}</div>}
            {endpoint.choices.map(choice => <button className={styles.splitChoice}
                key={`${choice.lane.index}:${choice.lane.version}`} disabled={!choice.eligible}
                aria-pressed={choice.selected}
                onClick={() => {
                    const selected = endpoint.choices.filter(lane => lane.selected).map(lane => lane.lane);
                    send(endpoint, choice.selected ? selected.filter(lane => !sameLane(lane, choice.lane)) : [...selected, choice.lane]);
                }}>
                {choice.selected ? "\u2713 " : ""}
                {label(laneLabel(choice, endpoint.choices).key, laneLabel(choice, endpoint.choices).fallback)
                    .replace("{number}", String(laneOrdinal(choice, endpoint.choices)))}
                {new Set(endpoint.choices.map(x => x.carriageway)).size > 1 && ` - ${label("LaneCarriageway", "Carriageway")} ${choice.carriageway + 1}`}
                {choice.reason && <div>{reasonLabel(choice.reason)}</div>}
            </button>)}
            {endpoint.choices.some(choice => choice.selected) && <button className={styles.splitChoice} onClick={() => send(endpoint, [])}>
                {label("LaneClearShort", "Clear selection")}
            </button>}
        </div>)}
    </>;
};
