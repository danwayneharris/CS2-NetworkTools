# Parameter metadata generator

This standalone .NET 8 program parses the mod Systems source and emits TypeScript.
It does not load game assemblies, deploy a mod, or fully compile C# semantics.

`dotnet run --project NetworkTools.Codegen -- NetworkTools.Mod/Systems <output.ts> --configuration Debug`

Supported configurations are Debug, Release (default), and I18N. Parsing supplies
TRACE plus the pinned Common project symbols: DEBUG/IS_DEBUG/ENABLE_PROFILER for
Debug, USE_BURST for Release, IS_DEBUG/ENABLE_PROFILER/EXPORT_EN_US for I18N.
`--define 'CUSTOM;OTHER'` adds symbols. Update this mapping if build symbols change.
The existing configuration-specific disabled-option emission is retained.

The accepted metadata subset is deliberate:

- Public fields of the existing parameter types with direct target-typed or explicit
  constructor initializers; required defaults/ranges must be present.
- Literal dotted keys/labels and literal numeric/boolean metadata. Keys and emitted
  short keys must be unique. Optional metadata after modes must use named arguments.
  Object initializer callbacks remain outside the emitted metadata contract.
- Enum member references or integer values for enum defaults; modes permit integer
  literals and `(int)Enum.Member` terms combined with `|`. Referenced enums must use
  integer literal members (or automatic increments); unresolved names, ambiguous
  simple names and unsupported computed member expressions fail.
- EnumOption takes literal label/icon and supported literal property values.
- Float3/quaternion defaults are deliberately not emitted, as before. This tool does
  not validate their runtime expression semantics or replace the C# compiler.

Unsupported emitted expressions produce NTGEN001 with a source location where
available, return nonzero and leave any existing output untouched. Empty parameter
results fail rather than overwrite generated metadata with an empty file. Syntax
errors are reported too. This is a bounded syntax extractor, not a semantic compiler;
constant expression evaluation and arbitrary qualified/aliased type resolution are
not claimed.

Run `scripts/test-codegen.ps1` for generator-only build/tests. It builds this project
and invokes `scripts/check-codegen.py`; neither command builds/deploys the mod.
Tests compare the complete line-order-independent current Debug/Release output
against pre-change hashes, check golden metadata values/configuration branches, and
prove malformed input cannot replace a previous output. Intentional parameter
changes require reviewing and updating those recorded output expectations.
