import React from "react";
import styles from "../toolActionPanel.module.scss";
import { ConnectMode, PARAM_KEYS, PARAM_BINDINGS } from "generated/parameters.generated";
import { useValue } from "cs2/api";
import { PrefabSelection } from "../shared/prefabSelection";
import { ParameterField } from "../shared/parameterField";
import { TabBar } from "../shared/tabBar";
import { LaneDirectionControls } from "./laneDirection";

import { GAME_BINDINGS, GAME_TRIGGERS } from "gameBindings";
import { useLocalization } from "cs2/l10n";

type ProfileChoice = { index: number; version: number; label: string; selected: boolean };
type ProfileContext = { endpoint: number; node: { index: number; version: number }; choices: ProfileChoice[]; reason: string | null };
const C = PARAM_BINDINGS.connect;

const profileStatusMessage = (reason: string): [string, string] => {
    if (reason === "profile_disabled") return ["ProfileDisabled", "Smooth elevation profile is off."];
    if (reason === "accepted") return ["ProfileAccepted", "Current preview accepted for Apply."];
    if (reason === "profile_mode_unsupported") return ["ProfileLoop", "Smooth elevation profile is unavailable for Loop."];
    if (reason === "profile_endpoint_height_offset_unsupported") return ["ProfileOffset", "The approach curve and node have different endpoint heights. This attachment is not supported."];
    if (reason === "profile_endpoint_moved_unsupported") return ["ProfileMoved", "Profile endpoints must stay at the selected nodes. Restore the endpoint handles."];
    if (reason === "profile_approach_required") return ["ProfileChoose", "Choose an approach edge."];
    if (reason.includes("native") && reason.includes("pending")) return ["ProfileNativePending", "Waiting for native profile validation. Apply is unavailable."];
    if (reason === "native_error") return ["ProfileNativeError", "The game rejected the current preview. Apply is unavailable."];
    if (reason.includes("native")) return ["ProfileNativeMismatch", "The native preview does not preserve the requested profile. Apply is unavailable."];
    if (["preview_pending", "preview_unavailable", "candidate_unavailable", "preview_changed", "stale_submission", "inputs_changed"].includes(reason))
        return ["ProfilePending", "Waiting for a current, stable preview. Apply is unavailable."];
    if (["inputs_unavailable", "profile_endpoint_unavailable", "profile_approach_missing", "profile_approach_invalid", "profile_approach_stale", "connect_not_active"].includes(reason))
        return ["ProfileUnavailable", "Approach context is unavailable or changed. Reselect the endpoint or approach."];
    return ["ProfileRejected", "The requested profile cannot be applied. Review the endpoint choices and curve handles."];
};

export const ConnectControls: React.FC = () => {
    const activeConnectMode = useValue(C.mode.binding) as ConnectMode;
    const available = useValue(GAME_BINDINGS.CONNECT_PROFILE_AVAILABLE.binding);
    const statusJson = useValue(GAME_BINDINGS.CONNECT_PROFILE_STATUS.binding);
    const status: { reason?: string; accepted?: boolean } = React.useMemo(() => JSON.parse(statusJson), [statusJson]);
    const [statusKey, statusFallback] = profileStatusMessage(status.reason || "preview_pending");
    const contextJson = useValue(GAME_BINDINGS.CONNECT_PROFILE_CONTEXT.binding);
    const contexts: ProfileContext[] = React.useMemo(() => JSON.parse(contextJson), [contextJson]);
    const { translate } = useLocalization();

    return (
        <>
            <div className={styles.section}>
                <div className={styles.section__tabs}>
                    <TabBar paramKey="connect.mode" />
                </div>
                <div className={styles.section__content}>
                    <PrefabSelection paramKey={PARAM_KEYS.connect.netPrefab} />
                    {available && (activeConnectMode === ConnectMode.Loop
                        ? <div>{translate("NetworkTools.UI.Connect.ProfileLoop", "Smooth elevation profile is unavailable for Loop.")}</div>
                        : <>
                            <ParameterField paramKey="connect.smoothElevationProfile" />
                            <div>{translate(`NetworkTools.UI.Connect.${statusKey}`, statusFallback)}
                                {status.reason && !["accepted", "profile_disabled"].includes(status.reason) && <div>{status.reason}</div>}
                            </div>
                            <div>{translate("NetworkTools.UI.Connect.ProfileExplanation", "Match endpoint heights and approach grades. Choose the existing approach at each junction. Native validation may reject profiles it cannot preserve.")}</div>
                            {contexts.map(context => <div className={styles.splitChoices} key={context.endpoint}>
                                <div>{translate(context.endpoint === 0 ? "NetworkTools.UI.Connect.StartApproach" : "NetworkTools.UI.Connect.EndApproach", context.endpoint === 0 ? "Start approach" : "End approach")}</div>
                                {context.reason && <div>{translate(context.reason === "profile_approach_required" ? "NetworkTools.UI.Connect.ProfileChoose" : "NetworkTools.UI.Connect.ProfileUnavailable", context.reason === "profile_approach_required" ? "Choose an approach edge." : "Approach context is unavailable or changed. Reselect the endpoint or approach.")}</div>}
                                {context.choices.map(choice => <button className={styles.splitChoice} key={`${choice.index}:${choice.version}`}
                                    aria-pressed={choice.selected}
                                    onClick={() => GAME_TRIGGERS.SET_CONNECT_PROFILE_APPROACH(JSON.stringify({ endpoint: context.endpoint, node: context.node, edge: { index: choice.index, version: choice.version } }))}>
                                    {choice.selected ? "[selected] " : ""}{choice.label}
                                </button>)}
                            </div>)}
                        </>)}
                    <LaneDirectionControls />
                    {activeConnectMode === ConnectMode.Loop && (
                        <ParameterField paramKey="connect.loopArcSide" />
                    )}
                </div>
            </div>
        </>
    );
};
