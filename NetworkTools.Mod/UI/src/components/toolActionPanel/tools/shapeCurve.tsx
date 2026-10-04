import React from "react";
import styles from "../toolActionPanel.module.scss";
import { ShapeTransformTemplate, PARAM_BINDINGS } from "generated/parameters.generated";
import { useValue } from "cs2/api";
import { TabBar } from "../shared/tabBar";
import { ParameterField } from "../shared/parameterField";
import { GAME_BINDINGS, GAME_TRIGGERS } from "gameBindings";
import { useLocalization } from "cs2/l10n";
import { VC } from "components/vanilla/Components";

// Quantize only the slider; numeric entry preserves finer values in meters.
const JunctionElevationLimitControls: React.FC = () => {
    const parameter = PARAM_BINDINGS.roadShape.junctionElevationLimit;
    const value = useValue(parameter.binding);
    const { translate } = useLocalization();
    const setValue = (next: number) => {
        if (Number.isFinite(next) && next >= 0 && next <= 20) parameter.set(next);
    };
    return <>
        <div className={styles.controlRow}><div className={styles.vanillaField}>
            <VC.FloatSliderField value={value} min={0} max={20} fractionDigits={1}
                label={translate("NetworkTools.UI.Curve.JunctionElevationLimit", "Maximum junction elevation change (m)")}
                onChange={(next: number) => setValue(Math.round(next * 2) / 2)} />
        </div></div>
        <div className={styles.controlRow}><div className={styles.vanillaField}>
            <VC.FloatInputField value={value} min={0} max={20} fractionDigits={3}
                label={translate("NetworkTools.UI.Curve.JunctionElevationPrecise", "Precise maximum change (m)")}
                onChange={setValue} />
        </div></div>
    </>;
};

type SplitChoice = { index: number; version: number; ordinal: number; distance: number; selected: boolean; eligible: boolean };

export const ShapeCurveControls: React.FC = () => {
    const template = useValue(PARAM_BINDINGS.roadShape.template.binding);
    const combinedAvailable = useValue(GAME_BINDINGS.COMBINED_SMOOTH_AVAILABLE.binding);
    const combined = useValue(PARAM_BINDINGS.roadShape.combinedSlope.binding);
    const allowJunctionElevation = useValue(PARAM_BINDINGS.roadShape.allowInteriorJunctionElevation.binding);
    const unlimitedJunctionElevation = useValue(PARAM_BINDINGS.roadShape.junctionElevationUnlimited.binding);
    const splitJson = useValue(GAME_BINDINGS.SPLIT_CHOICES.binding);
    const splits: SplitChoice[] = React.useMemo(() => JSON.parse(splitJson), [splitJson]);
    const { translate } = useLocalization();

    return (
        <div className={styles.section}>
            <div className={styles.section__tabs}>
                <TabBar paramKey="roadShape.template" group="Curve" />
            </div>
            {template === ShapeTransformTemplate.CurveSmooth && (
                <div className={styles.section__content}>
                    <ParameterField paramKey="roadShape.smoothingFactor" />
                    {combinedAvailable && <ParameterField paramKey="roadShape.combinedSlope" />}
                    {combinedAvailable && combined && <>
                        <ParameterField paramKey="roadShape.smoothStart" />
                        <ParameterField paramKey="roadShape.smoothEnd" />
                        <ParameterField paramKey="roadShape.allowInteriorJunctionElevation" />
                        {allowJunctionElevation && <ParameterField paramKey="roadShape.junctionElevationUnlimited" />}
                        {allowJunctionElevation && !unlimitedJunctionElevation && <JunctionElevationLimitControls />}
                        <div>{translate("NetworkTools.UI.Curve.JunctionElevationExplanation",
                            "Limit movement above or below the original junction height for this operation. Disabled or zero keeps the original height. Bounds may prevent a constant grade; endpoints and split pins remain fixed.")}</div>
                    </>}
                    {combinedAvailable && combined && <div>{translate("NetworkTools.UI.Curve.CombinedExplanation",
                        "Fit slope along the smoothed path. Endpoints and split points stay fixed; interior junction elevation follows its permission and limit. Conflicting split grades cannot be applied.")}</div>}
                    {splits.length > 0 && <div className={styles.splitChoices}>
                        <div>{translate("NetworkTools.UI.Curve.SplitPoints", "Split points")}</div>
                        <div>{translate("NetworkTools.UI.Curve.SplitExplanation", "Pin a node and align its join at every strength, including zero.")}</div>
                        {splits.map(node => <button className={styles.splitChoice} key={`${node.index}:${node.version}`}
                            disabled={!node.eligible} aria-pressed={node.selected}
                            onClick={() => GAME_TRIGGERS.SET_SPLIT_NODE(node.index, node.version, !node.selected)}>
                            {node.selected ? "● " : "○ "}{translate("NetworkTools.UI.Curve.Node", "Node")} {node.ordinal} · {node.distance} m
                        </button>)}
                    </div>}
                </div>
            )}
        </div>
    );
};
