# WrenSharp.SourceGenerator — TODO

## Bugs (wrong behaviour / crashes)

- [ ] **generic classes** aren't handled (the partial declaration needs its containing types).
- [ ] **Wren keywords as parameter names.** C# parameters named `is`, `in`, `var`, `this`, … produce invalid Wren.
      Emit generic names (`a0`, `a1`, …) in the Wren source.

## Missing features

- [ ] **More types:**
  - [ ] `long`, `short`, `byte`, `uint`, `ulong`, … (`GetArgInt64`/`GetArgInt16`/… already exist)
  - [ ] `List<T>` / `Dictionary<TKey, TValue>` ↔ Wren list/map
- [ ] **Private setters**: Allow specifying in the `WrenProperty` which getters and setters are allowed.

## Diagnostics

- [ ] Unsupported parameter/return/property types (currently fall back to `GetArgSharedData<T>` silently).
- [ ] Returning a `[WrenClass]` type whose module has no binding (leads to a native crash in `ReturnForeign`).
- [ ] Report WREN002 (duplicate Wren signature) — defined but never reported.
- [ ] `init`-only setters (generated code doesn't compile).
- [ ] Replace `throw` in `Analyze` with a diagnostic.
- [ ] Fix nullability warnings (CS8604) in `WrenInterop.analyze.cs`.

## Tests

- [ ] Snapshot tests of generated output (e.g. Verify.SourceGenerators).
- [ ] End-to-end tests in `WrenSharp.Tests` running Wren against generated classes:
  - [ ] Operators return new objects and don't change their operands
  - [ ] Multi-argument signatures (`equals(_,_)`)
  - [ ] `toString` getter, string interpolation, `System.print`
  - [ ] Shared data is released after GC (finalizer)
  - [ ] Static properties and static properties of a `[WrenClass]` type
  - [ ] Overload resolution by argument type
