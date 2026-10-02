# F08 focused slice: finite parameter boundary

Current `Parameter<T>.SetValue` accepted NaN/infinity from code, handles, dependencies,
UI setters and persisted scalar strings. Invalid numbers therefore could become
normal state and raise OnChanged before geometry validation. The safe correction
is shared validation before mutation, not global enforcement of UI ranges.

## Implemented

- Added `TrySetValue` while retaining the void `SetValue` and `Value` APIs. All
  routes reject nonfinite floats/doubles and components of the current vector
  parameter types (`float3`, `quaternion`), retaining previous state without
  OnChanged. Other types retain their existing behavior.
- Invalid developer-authored defaults throw at construction rather than seeding
  invalid state; existing finite zero quaternion defaults remain allowed. This
  is not a normalization or range rule.
- Persisted parse errors and nonfinite values explicitly log rejection and return
  false, retaining old/default state. Successful parses call the same boundary.
- Parse error handling no longer catches a subscriber exception after a successful
  mutation and mislabels it as a failed parse. Subscriber failures propagate.
- Corrected ResetToDefault documentation: unchanged values are equality-guarded.
- No enum membership, prefab validity or numeric-range policies were invented.
  F08 remains partly deferred, not wholly fixed by this narrow change.

## UI integration handoff

`Common/Extensions/ValueBindingHelper.cs` updates its binding before invoking the
callback. Merely rejecting a parameter write would therefore leave the binding
optimistically invalid. Parent owns `UISystem.cs`: its float binding should use
TrySetValue and restore `b.Value = fp.Value` on false. Do not ForceNotify after a
rejection (that would violate the no-OnChanged invariant). No submodule edit is
needed. Rendering/transport remains a runtime verification item.

## Tests

`dotnet run --project NetworkTools.Parameter.Tests`: **52 assertions passed**.
The executable links real parameter source with minimal Unity value/logging/handle
metadata doubles. Tests cover all mutation origins, scalar nonfinite values,
each current vector/quaternion component, malformed persistence and overflow,
retained state/notification count, finite out-of-range handle values, resets and
subscriber exceptions. Test doubles do not establish actual Gameface behavior or
Unity/Burst execution. One expected C# warning concerns the Unity-style lowercase
`quaternion` name in its test double.

Full mod compilation and the UI integration are owned by the parent sprint.
Instrumentation stays available: rejected writes and parsing failures have explicit
warnings with key and origin/error category, without echoing raw persisted contents.
