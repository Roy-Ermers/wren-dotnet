# Wren for .NET

Expose C# types to the [Wren](https://wren.io/) scripting language with a source generator. Mark a type with
`[WrenType]`, and the generator writes the Wren declaration and a binding built directly on Wren's C API:

```cs
[WrenType]
public partial class Vector3
{
  [WrenConstructor]
  public Vector3(int x, int y, int z) => (X, Y, Z) = (x, y, z);

  [WrenProperty] public int X { get; set; }
  [WrenProperty] public int Y { get; set; }
  [WrenProperty] public int Z { get; set; }

  [WrenMethod]
  public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

  [WrenMethod]
  public override string ToString() => $"({X}, {Y}, {Z})";
}
```

```wren
import "engine" for Vector3

var v = Vector3.new(1, 2, 3) + Vector3.new(1, 1, 1)
v.x = 10
System.print(v) // (10, 3, 4)
```

- **Fast**: Wren calls each binding directly as an `[UnmanagedCallersOnly]` function, without reflection or
  allocations per call.
- **Upstream Wren**: only the public Wren 0.4 API (`wren.h`) is used.
- **Checked**: arguments are type-checked at runtime, and mistakes in bindings are compile errors.

## Projects

| Project                | Description                                                     |
| ---------------------- | --------------------------------------------------------------- |
| `Wren.Native`          | The Wren C API bindings, the `WrenVm` host and native libraries |
| `Wren.SourceGenerator` | The source generator and analyzer                               |
| `Wren.Examples`        | A sample console app                                            |
| `Wren.Tests`           | Tests (`dotnet test`)                                           |

## Getting started

Reference `Wren.Native`, add the generator as an analyzer, and enable unsafe code:

```xml
<PropertyGroup>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>

<ItemGroup>
  <ProjectReference Include="..\Wren.Native\Wren.Native.csproj" />
  <ProjectReference Include="..\Wren.SourceGenerator\Wren.SourceGenerator.csproj"
                    OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

Or use the `Wren` NuGet package, which includes the generator and the native libraries (Windows, Linux and macOS, x64
and arm64). Tagged releases are published to this repository's GitHub Packages feed; `dotnet pack Wren.Native -c Release`
builds it locally.

Then create a VM, add the generated binding, and run some Wren. `WrenBinding` is generated in your project's root
namespace, so a library can expose its Wren types to the apps using it:

```cs
using Wren;

using var vm = new WrenVm(new WrenVmOptions { Write = Console.Write });
vm.AddBinding(WrenBinding.Instance);

vm.Interpret("main", """
  import "engine" for Vector3
  System.print(Vector3.new(1, 2, 3))
  """);
```

## What can be exposed

- **Types**: classes, structs, records and enums, with `[WrenType]` or `[WrenType("module", "ClassName")]`.
- **Members**: methods, operators, properties, fields, indexers and constructors, opt-in with `[WrenMethod]`,
  `[WrenProperty]` and `[WrenConstructor]`. A record's primary constructor and positional properties need no
  attribute.
- **Values**: `bool`, numbers, `string`, enums, `[WrenType]` objects, and collections of them (copied
  to Wren lists and maps). Wren functions can be passed to C# as delegates.
- **Errors**: a C# exception becomes a Wren runtime error, and Wren errors are thrown as a `WrenException`.

Mistakes are reported as compile errors (`WREN001`–`WREN014`), each explaining what to change.

## License

MIT, see [LICENSE](LICENSE). Originally based on WrenSharp by Steve Woolcock.
For more on the Wren language, see [wren.io](https://wren.io/).
