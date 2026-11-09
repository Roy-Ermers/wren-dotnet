using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Wren.SourceGenerator;

namespace Wren.Tests;

public class DiagnosticTests
{
  private const string Valid = """
      [WrenType]
      public partial class Thing
      {
          [WrenConstructor] public Thing(int size) => Size = size;
          [WrenProperty] public int Size { get; set; }
          [WrenMethod] public string Describe(string? prefix) => prefix + Size;
      }
      """;

  [Test]
  [Arguments("WREN001", "[WrenType] public class Thing { }", true)]
  [Arguments("WREN002", """
      [WrenType]
      public partial class Thing
      {
          [WrenProperty("toString")] public string Text { get; } = "";
          [WrenMethod] public override string ToString() => Text;
      }
      """, true)]
  [Arguments("WREN009", """
      [WrenType]
      public partial class Thing
      {
          [WrenMethod] public void Get(out int size) => size = 1;
      }
      """, true)]
  [Arguments("WREN010", """
      [WrenType]
      public partial class Thing
      {
          [WrenMethod] public object Get() => new();
      }
      """, true)]
  [Arguments("WREN011", """
      [WrenType]
      public partial class Thing
      {
          [WrenMethod] public void Log(int value) { }
          [WrenMethod] public void Log(double value) { }
      }
      """, true)]
  [Arguments("WREN013", Valid, false)]
  [Arguments("WREN010", """
      [WrenType]
      public partial class Thing
      {
          [WrenMethod] public void Take(List<object> items) { }
      }
      """, true)]
  [Arguments("WREN010", """
      [WrenType]
      public partial class Thing
      {
          [WrenMethod] public void Take(System.Collections.Immutable.ImmutableArray<int> items) { }
      }
      """, true)]
  [Arguments("WREN014", """
      [WrenType]
      public partial class Thing
      {
          [WrenMethod] public void Take(List<Dictionary<string, int>> scores) { }
      }
      """, true)]
  public async Task Reports_diagnostic(string id, string source, bool allowUnsafe)
  {
    var (diagnostics, _) = RunGenerator(source, allowUnsafe);

    await Assert.That(diagnostics.Select(d => d.Id)).Contains(id);
  }

  [Test]
  public async Task Valid_type_compiles_without_diagnostics()
  {
    var (diagnostics, output) = RunGenerator(Valid, allowUnsafe: true);

    await Assert.That(diagnostics.Select(d => d.ToString())).IsEmpty();
    await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)
        .Select(d => d.ToString())).IsEmpty();
  }

  [Test]
  public async Task Binding_is_public_in_a_namespace_named_after_the_assembly()
  {
    var (_, output) = RunGenerator(Valid, allowUnsafe: true, assemblyName: "My-Game.Scripts");

    var binding = output.GetTypeByMetadataName("My_Game.Scripts.WrenBinding");

    await Assert.That(binding?.DeclaredAccessibility).IsEqualTo(Accessibility.Public);
    await Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)
        .Select(d => d.ToString())).IsEmpty();
  }

  private static (ImmutableArray<Diagnostic> Diagnostics, Compilation Output) RunGenerator(string source,
      bool allowUnsafe, string assemblyName = "Snippet")
  {
    var compilation = CSharpCompilation.Create(
        assemblyName,
        [CSharpSyntaxTree.ParseText("using System;\nusing System.Collections.Generic;\nusing Wren;\n\n" + source)],
        References(),
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
            allowUnsafe: allowUnsafe, nullableContextOptions: NullableContextOptions.Enable));

    CSharpGeneratorDriver.Create(new WrenInterop())
        .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

    return (diagnostics, output);
  }

  // Excludes this assembly, which has its own generated types.
  private static IEnumerable<MetadataReference> References()
  {
    var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Append(typeof(WrenVm).Assembly.Location)
        .Where(path => path != typeof(DiagnosticTests).Assembly.Location)
        .Distinct();

    return paths.Select(path => MetadataReference.CreateFromFile(path));
  }
}
