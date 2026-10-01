import React from "react";
import { Button, Tooltip } from "cs2/ui";
import { useValue } from "cs2/api";
import { GAME_BINDINGS } from "gameBindings";
import styles from "./editorInjection.module.scss";
import { useLocalization } from "cs2/l10n";
import { NetworkToolsWrapper } from "components/wrapper/wrapper";

export const EditorInjection = () => {
    const panelOpenBinding = useValue(GAME_BINDINGS.PANEL_OPEN.binding);
    const { translate } = useLocalization();

    return (
        <>
            <div className={styles.buttonWrapper}>
                <Tooltip
                    tooltip={`${translate("NetworkTools.UI.Common.NetworkTools")} [Dan local]`}
                    delayTime={0}
                    direction="down">
                    <Button
                        variant="floating"
                        onSelect={() => GAME_BINDINGS.PANEL_OPEN.set(!panelOpenBinding)}
                        src={"coui://nt/Logo.svg"}
                    />
                </Tooltip>
            </div>
            <div className={styles.editorWrapper}>
                <NetworkToolsWrapper />
            </div>
        </>
    );
};
