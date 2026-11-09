using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Wren.SourceGenerator.Attributes;
using Wren.SourceGenerator.Models;

namespace Wren.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public partial class WrenInterop : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    context.RegisterPostInitializationOutput(static postInitializationContext =>
    {
      postInitializationContext.AddEmbeddedAttributeDefinition();
      postInitializationContext.AddSource("WrenType", Templates.WrenTypeAttribute);
      postInitializationContext.AddSource("WrenProperty", Templates.WrenPropertyAttribute);
      postInitializationContext.AddSource("WrenMethod", Templates.WrenMethodAttribute);
      postInitializationContext.AddSource("WrenConstructor", Templates.WrenConstructorAttribute);
    });

    var pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
        fullyQualifiedMetadataName: $"{Templates.Namespace}.WrenTypeAttribute",
        predicate: static (node, cancellationToken) =>
            node is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
        transform: static (ctx, _) => Analyze(ctx)
    ).WithTrackingName(TrackingNames.ClassModels);

    var bindingNamespace = context.AnalyzerConfigOptionsProvider
        .Combine(context.CompilationProvider.Select(static (compilation, _) => compilation.AssemblyName))
        .Select(static (target, _) => BindingNamespace(target.Left.GlobalOptions, target.Right));

    context.RegisterSourceOutput(pipeline.Combine(bindingNamespace),
        static (spc, target) => EmitClass(spc, target.Left, target.Right));

    var allClasses = pipeline
        .Where(static model => !model.HasErrors)
        .Select(static (model, _) => new WrenBindingEntry(
            model.Module, model.Name, model.FullyQualifiedName, Depth: model.Depth, IsStatic: model.IsStatic))
        .WithTrackingName(TrackingNames.BindingEntries)
        .Collect();

    RegisterUnsafeCheck(context, allClasses);

    // Enums can't be partial, so their Wren source is embedded in the binding instead of a generated C# file.
    var enumPipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
        fullyQualifiedMetadataName: $"{Templates.Namespace}.WrenTypeAttribute",
        predicate: static (node, cancellationToken) => node is EnumDeclarationSyntax,
        transform: static (ctx, _) => AnalyzeEnum(ctx)
    ).WithTrackingName(TrackingNames.EnumModels);

    context.RegisterSourceOutput(enumPipeline, static (spc, model) =>
    {
      foreach (var diagnostic in model.Diagnostics)
        spc.ReportDiagnostic(diagnostic.ToDiagnostic());
    });

    var allEnums = enumPipeline
        .Where(static model => !model.HasErrors)
        .Select(static (model, _) => new WrenBindingEntry(
            model.Module, model.Name, model.FullyQualifiedName, WrenCodeGenerator.GenerateEnum(model)))
        .Collect();

    context.RegisterSourceOutput(allClasses.Combine(allEnums).Combine(bindingNamespace), static (spc, target) =>
    {
      var entries = target.Left.Left.AddRange(target.Left.Right);
      if (entries.IsEmpty)
        return;

      spc.AddSource("WrenBinding.g.cs", Templates.BindingClass(entries, target.Right));
    });
  }

  // The binding is public, so it's generated in the project's root namespace: a fixed namespace would make the
  // bindings of different assemblies clash.
  private static string? BindingNamespace(AnalyzerConfigOptions options, string? assemblyName)
  {
    var name = options.TryGetValue("build_property.RootNamespace", out var rootNamespace) &&
               !string.IsNullOrWhiteSpace(rootNamespace)
        ? rootNamespace
        : assemblyName;

    if (string.IsNullOrWhiteSpace(name))
      return null;

    // Assembly names can contain characters identifiers can't.
    return string.Join(".", name!.Split('.').Select(static part =>
    {
      var identifier = new string(part.Select(static c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray());
      return identifier.Length == 0 || char.IsDigit(identifier[0]) ? "_" + identifier : identifier;
    }));
  }

  private static void RegisterUnsafeCheck(IncrementalGeneratorInitializationContext context,
      IncrementalValueProvider<ImmutableArray<WrenBindingEntry>> classes)
  {
    var allowUnsafe = context.CompilationProvider
        .Select(static (compilation, _) => compilation.Options is CSharpCompilationOptions { AllowUnsafe: true });

    context.RegisterSourceOutput(classes.Combine(allowUnsafe), static (spc, target) =>
    {
      if (!target.Right && !target.Left.IsEmpty)
        spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnsafeCodeRequired, Location.None));
    });
  }

  // Used by tests to assert that pipeline steps are cached.
  public static class TrackingNames
  {
    public const string ClassModels = nameof(ClassModels);
    public const string BindingEntries = nameof(BindingEntries);
    public const string EnumModels = nameof(EnumModels);
  }
}
