"""Audit repeat-Apply geometry; lane connectivity and native surfaces are separate checks."""
import argparse
import json
import math
from pathlib import Path


def audit(data, tolerance=.05):
    if not math.isfinite(tolerance) or tolerance <= 0:
        raise ValueError("Invalid tolerance")

    def indexed(rows):
        result = {(r["index"], r["version"]): r for r in rows}
        if not result or len(result) != len(rows):
            raise ValueError("Missing or duplicate identities")
        return result

    def distance(p, q):
        values = [v[k] for v in (p, q) for k in ("x", "y", "z")]
        if not all(math.isfinite(x) for x in values):
            raise ValueError("Nonfinite geometry")
        return math.dist(values[:3], values[3:])

    old, new = indexed(data["beforeEdges"]), indexed(data["afterEdges"])
    before, after = indexed(data["beforeNodes"]), indexed(data["afterNodes"])
    if old.keys() != new.keys() or before.keys() != after.keys():
        raise ValueError("Topology identity changed")
    topology = all(all(e[k] == new[key][k] for k in ("startNode", "endNode", "prefab")) for key, e in old.items())
    errors = []
    for key, e in old.items():
        if len(e["curve"]) != 4 or len(new[key]["curve"]) != 4:
            raise ValueError("Incomplete cubic")
        errors.extend(distance(p, q) for p, q in zip(e["curve"], new[key]["curve"]))
    control = max(errors)
    nodes = max(distance(n["position"], after[key]["position"]) for key, n in before.items())
    return {
        "toleranceMeters": tolerance, "maxControlDisplacement": control,
        "maxNodeDisplacement": nodes, "sameTopology": topology,
        "idempotentWithinTolerance": topology and max(control, nodes) <= tolerance,
        "limits": "Permanent authored geometry only. Directed lanes, generated surfaces and visual quality need separate checks.",
    }


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("comparison")
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    result = audit(json.loads(Path(args.comparison).read_text()))
    Path(args.output).write_text(json.dumps(result, indent=2))
    print(json.dumps(result, indent=2))
    if not result["idempotentWithinTolerance"]:
        raise SystemExit(1)
