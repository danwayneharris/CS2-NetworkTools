import React from "react";
import styles from "../toolActionPanel.module.scss";
import { ShapeTransformTemplate, PARAM_BINDINGS } from "generated/parameters.generated";
import { useValue } from "cs2/api";
import { TabBar } from "../shared/tabBar";
import { ParameterField } from "../shared/parameterField";
import { GAME_BINDINGS, GAME_TRIGGERS } from "gameBindings";
import { useLocalization } from "cs2/l10n";

type SplitChoice = { index: number; version: number; ordinal: number; distance: number; selected: boolean; eligible: boolean };

export const ShapeCurveControls: React.FC = () => {
    const template = useValue(PARAM_BINDINGS.roadShape.template.binding);
    const combinedAvailable = useValue(GAME_BINDINGS.COMBINED_SMOOTH_AVAILABLE.binding);
    const combined = useValue(PARAM_BINDINGS.roadShape.combinedSlope.binding);
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
                    {combinedAvailable && combined && <div>{translate("NetworkTools.UI.Curve.CombinedExplanation",
                        "Experimental: fit slope along the smoothed path. Endpoints, split points and junction heights stay fixed. Conflicting split grades cannot be applied.")}</div>}
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
