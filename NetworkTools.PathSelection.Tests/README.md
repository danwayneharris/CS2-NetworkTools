# Path selection regressions

Run `dotnet run --project NetworkTools.PathSelection.Tests` from the repository root.
This .NET 8 executable links the actual production PathFinding partial class. It
requires no game installation, packages, deployment, or live game. Failure throws
and returns a nonzero exit code.

Managed ECS/collection doubles supply a tiny graph. They deliberately do not claim
Unity allocation safety, native heap equal-cost order, Burst, or in-game selection
coverage. The comparator stays strictly cost-only; equal-cost tests require an
optimal route rather than an arbitrary new tie policy.

Coverage includes the incoming-prefab counterexample, both edge storage and search
orientations, disconnected cyclic graphs, equal-cost alternatives, same-node
legacy behavior, missing-prefab policy and 200 deterministic comparisons against
an independent exhaustive simple-path oracle on small known-prefab graphs.
