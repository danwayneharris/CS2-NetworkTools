# Parameter boundary regressions

Run `dotnet run --project NetworkTools.Parameter.Tests` from the repository root.
The executable links the production parameter classes; logging, handle metadata,
and Unity value types have minimal doubles. No game installation/deployment needed.

Tests cover nonfinite float/double, all float3/quaternion components, mutation
origins, notification suppression, invalid persisted data, finite writes beyond
UI range metadata, reset and overflow. They do not certify Gameface rendering,
actual binding transport, Unity/Burst, or range/enum/prefab validity policy.

The UI's optimistic binding needs resetting to the retained value after a rejected
write; that integration lives in UISystem and requires its own runtime check.
