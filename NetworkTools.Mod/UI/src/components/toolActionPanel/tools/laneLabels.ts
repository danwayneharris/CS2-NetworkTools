// DiagramPosition is oriented toward the junction; invert for outgoing traffic.
// Keep numbering within the same carriageway and travel direction, not composition IDs.
export type LabelLane = { index: number; position: number; incoming: boolean; carriageway: number };
export function laneLabel(choice: LabelLane, choices: LabelLane[]): { key: string; fallback: string } {
    const group = choices.filter(x => x.incoming === choice.incoming && x.carriageway === choice.carriageway)
        .sort((a, b) => (choice.incoming ? 1 : -1) * (a.position - b.position));
    const ordinal = group.indexOf(choice);
    if (ordinal < 0 || !group.every(x => Number.isFinite(x.position)) ||
        group.some((x, i) => i > 0 && Math.abs(x.position - group[i - 1].position) < 0.0001))
        return { key: "LanePositionUnknown", fallback: "Lane (position unavailable)" };
    if (group.length === 1) return { key: "LaneOnly", fallback: "Only lane" };
    if (ordinal === 0) return { key: "LaneLeftmost", fallback: "Leftmost lane" };
    if (ordinal === group.length - 1) return { key: "LaneRightmost", fallback: "Rightmost lane" };
    if (group.length === 3) return { key: "LaneMiddle", fallback: "Middle lane" };
    return { key: "LaneFromLeft", fallback: "Lane {number} from left" };
}
export function laneOrdinal(choice: LabelLane, choices: LabelLane[]): number {
    return choices.filter(x => x.incoming === choice.incoming && x.carriageway === choice.carriageway)
        .sort((a, b) => (choice.incoming ? 1 : -1) * (a.position - b.position)).indexOf(choice) + 1;
}
