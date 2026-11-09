using Wren.SourceGenerator.helpers;

namespace Wren.SourceGenerator.Models;

public enum WrenType
{
  Bool,
  Number,
  Foreign,
  List,
  Map,
  Null,
  String,
  Unknown
}

public enum WrenValueKind
{
  Other,
  Bool,
  Number,
  Float,
  Double,
  String,
  Char,
  Foreign,
  Delegate,
  Enum,
  List,
  Map,
}

public static class WrenTypeExtensions
{
  public static WrenType ToWrenType(this WrenValueKind kind) => kind switch
  {
    WrenValueKind.Bool => WrenType.Bool,
    WrenValueKind.Number or WrenValueKind.Float or WrenValueKind.Double or WrenValueKind.Enum => WrenType.Number,
    WrenValueKind.String or WrenValueKind.Char => WrenType.String,
    WrenValueKind.Foreign or WrenValueKind.Other => WrenType.Foreign,
    WrenValueKind.List => WrenType.List,
    WrenValueKind.Map => WrenType.Map,
    _ => WrenType.Unknown
  };
}

// Models must be value-equatable for the incremental pipeline: use EquatableArray, never symbols, syntax or Diagnostics.

public record WrenClassModel(
    string Name,
    string CsName,
    string Module,
    string? Namespace,
    string FullyQualifiedName,
    EquatableArray<string> ContainingTypes,
    EquatableArray<WrenMethod> Methods,
    EquatableArray<WrenProperty> Properties,
    EquatableArray<DiagnosticInfo> Diagnostics,
    int Depth = 0,
    bool IsStatic = false,
    bool HasWrenTypeBase = false,
    string Declaration = "",
    bool IsValueType = false)
{
  public bool HasErrors => Diagnostics.Any(d => d.IsError);

  public string HintName => FullyQualifiedName.Replace("global::", "") + ".WrenInterop.g.cs";
}

public record WrenBindingEntry(
    string Module,
    string ClassName,
    string FullyQualifiedName,
    string? InlineSource = null,
    int Depth = 0,
    bool IsStatic = false
);

public record WrenEnumModel(
    string Name,
    string Module,
    string FullyQualifiedName,
    EquatableArray<WrenEnumMember> Members,
    EquatableArray<DiagnosticInfo> Diagnostics)
{
  public bool HasErrors => Diagnostics.Any(d => d.IsError);
}

public record WrenEnumMember(
    string Name,
    string Value
);

public record WrenParameter(
    string Name,
    string Type,
    WrenValueKind Kind,
    WrenDelegateInfo? Delegate = null,
    bool Nullable = false,
    WrenCollection? Collection = null
)
{
  public bool IsForeign => Kind == WrenValueKind.Foreign;
}

public record WrenValueType(
    string Type,
    WrenValueKind Kind,
    WrenForeignType? Foreign = null,
    bool Nullable = false,
    WrenCollection? Collection = null
);

public enum WrenCollectionShape
{
  List,
  Array,
  Dictionary,
}

/// <param name="ConstructedType">Set if the collection read from Wren must be passed to this type's constructor, e.g. <c>HashSet&lt;int&gt;</c>.</param>
/// <param name="CanRead">False if there's no way to create the collection from Wren.</param>
public record WrenCollection(
    WrenCollectionShape Shape,
    WrenValueType Element,
    WrenValueType? Key = null,
    string? ConstructedType = null,
    bool CanRead = true,
    bool IsValueType = false
)
{
  public bool IsConstructible => CanRead && Element.Collection?.IsConstructible != false;
  public bool HasNestedMap => ContainsMap(Element);

  private static bool ContainsMap(WrenValueType value)
    => value.Kind == WrenValueKind.Map || value.Collection is { } collection && ContainsMap(collection.Element);
}

/// <summary>
/// Some bindings are a Wren method calling a foreign one, because Wren's C API can't list a map's keys (so they're
/// passed as lists), and a foreign method can't safely grow slots (<c>wrenEnsureSlots</c> may reallocate the stack
/// under the interpreter), so Wren reserves them by passing null arguments:
/// <code>
/// add(scores) { add_(scores, scores is Map ? scores.keys.toList : null, null, null) }
/// foreign add_(scores, scoresKeys, slot0, slot1)
/// </code>
/// </summary>
public sealed class WrenBindingSite
{
  private WrenBindingSite(bool isStatic, string header, string signature, string[] parameterNames,
      int[] mapPositions, int extraSlots, string wrapperName)
  {
    IsStatic = isStatic;
    Header = header;
    Signature = signature;
    ParameterNames = parameterNames;
    MapPositions = mapPositions;
    ExtraSlots = extraSlots;
    WrapperName = wrapperName;
  }

  public bool IsStatic { get; }

  public string Header { get; }

  public string Signature { get; }

  public string[] ParameterNames { get; }

  public int[] MapPositions { get; }

  public int ExtraSlots { get; }

  public string WrapperName { get; }

  public bool IsWrapped => MapPositions.Length > 0 || ExtraSlots > 0;

  public int Arity => ParameterNames.Length;

  public int KeysSlot => Arity + 1;

  public int FreeSlot => Arity + 1 + MapPositions.Length;

  public string ForeignSignature => IsWrapped
      ? WrapperName + "(" + Placeholders(Arity + MapPositions.Length + ExtraSlots) + ")"
      : Signature;

  public string[] Declaration
  {
    get
    {
      var prefix = IsStatic ? "static " : "";
      if (!IsWrapped)
        return [$"foreign {prefix}{Header}"];

      var keys = MapPositions.Select(i => $"{ParameterNames[i]} is Map ? {ParameterNames[i]}.keys.toList : null");
      var padding = Enumerable.Repeat("null", ExtraSlots);
      var arguments = ParameterNames.Concat(keys).Concat(padding);

      var parameters = ParameterNames
          .Concat(MapPositions.Select(i => ParameterNames[i] + "Keys"))
          .Concat(Enumerable.Range(0, ExtraSlots).Select(i => "slot" + i));

      return
      [
          $"{prefix}{Header} {{ {WrapperName}({string.Join(", ", arguments)}) }}",
          $"foreign {prefix}{WrapperName}({string.Join(", ", parameters)})"
      ];
    }
  }

  public static WrenBindingSite ForMethods(IReadOnlyList<WrenMethod> overloads)
  {
    var first = overloads[0];
    var names = first.Parameters.Select(p => WrenHelpers.ConvertNameToWrenStyle(p.Name)).ToArray();

    var header = first.Indexer switch
    {
      WrenIndexer.Get => "[" + string.Join(", ", names) + "]",
      WrenIndexer.Set => "[" + string.Join(", ", names.Take(names.Length - 1)) + "]=(" + names[names.Length - 1] + ")",
      _ when first.IsGetter => first.WrenName,
      _ => first.WrenName + "(" + string.Join(", ", names) + ")"
    };

    var wrapperName = first switch
    {
      { Indexer: WrenIndexer.Get } => "subscript_",
      { Indexer: WrenIndexer.Set } => "subscriptSet_",
      { IsOperator: true } => "op" + OperatorWord(first.Name, unary: first.IsGetter) + "_",
      _ => first.WrenName + "_"
    };

    var mapPositions = overloads
        .SelectMany(method => method.Parameters.Select((parameter, index) => (parameter, index)))
        .Where(p => p.parameter.Kind == WrenValueKind.Map)
        .Select(p => p.index)
        .Distinct()
        .OrderBy(index => index)
        .ToArray();

    // The result is written after the arguments are consumed, so it can reuse their slots.
    var read = overloads.SelectMany(m => m.Parameters).Select(p => ReadSlots(p.Collection)).DefaultIfEmpty(0).Max();
    var write = overloads.Select(m => WriteSlots(m.ReturnCollection)).Max();
    var extra = Math.Max(Math.Max(read, write - names.Length - mapPositions.Length), 0);

    return new(first.IsStatic, header, first.GetSignature(), names, mapPositions, extra, wrapperName);
  }

  public static WrenBindingSite ForGetter(WrenProperty property)
  {
    var name = WrenHelpers.ConvertNameToWrenStyle(property.Name);
    return new(property.IsStatic, name, name, [], [], WriteSlots(property.Collection), name + "_");
  }

  public static WrenBindingSite ForSetter(WrenProperty property)
  {
    var name = WrenHelpers.ConvertNameToWrenStyle(property.Name);
    int[] mapPositions = property.Kind == WrenValueKind.Map ? [0] : [];
    return new(property.IsStatic, name + "=(value)", name + "=(_)", ["value"], mapPositions,
        ReadSlots(property.Collection), name + "Set_");
  }

  private static int ReadSlots(WrenCollection? collection)
    => collection is null ? 0 : (collection.Key is null ? 1 : 2) + ReadSlots(collection.Element.Collection);

  private static int WriteSlots(WrenCollection? collection)
    => collection is null ? 0 : (collection.Key is null ? 1 : 2) + WriteSlots(collection.Element);

  private static int WriteSlots(WrenValueType value)
    => value.Collection is not null ? WriteSlots(value.Collection) : value.Foreign is not null ? 1 : 0;

  private static string OperatorWord(string op, bool unary) => op switch
  {
    "-" when unary => "Negate",
    "!" => "Not",
    "~" => "Complement",
    "+" => "Plus",
    "-" => "Minus",
    "*" => "Times",
    "/" => "Divide",
    "%" => "Modulo",
    "==" => "Equals",
    "!=" => "NotEquals",
    "<" => "Less",
    "<=" => "LessOrEqual",
    ">" => "Greater",
    ">=" => "GreaterOrEqual",
    "&" => "And",
    "|" => "Or",
    "^" => "Xor",
    "<<" => "ShiftLeft",
    ">>" => "ShiftRight",
    _ => "Operator"
  };

  private static string Placeholders(int count) => string.Join(",", Enumerable.Repeat("_", count));
}

public record WrenDelegateInfo(
    string Type,
    EquatableArray<WrenValueType> Parameters,
    WrenValueType? Return
)
{
  public string HelperName => "__WrenCallback_" + new string(
      Type.Replace("global::", "").Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
}

public record WrenProperty(
    string Name,
    string CsName,
    string Type,
    WrenValueKind Kind,
    bool Getter,
    bool Setter,
    bool IsStatic = false,
    WrenForeignType? Foreign = null,
    WrenDelegateInfo? Delegate = null,
    bool Nullable = false,
    WrenCollection? Collection = null
);

public record WrenForeignType(
    string Module,
    string ClassName,
    bool IsValueType = false
);

public record WrenMethod(
    string Name,
    string CsMethodName,
    bool IsStatic,
    bool IsOperator,
    string ReturnType,
    EquatableArray<WrenParameter> Parameters,
    WrenForeignType? ReturnForeign = null,
    bool IsGetter = false,
    WrenIndexer Indexer = WrenIndexer.None,
    WrenValueKind ReturnKind = WrenValueKind.Other,
    bool ReturnNullable = false,
    WrenCollection? ReturnCollection = null,
    bool IsConstructor = false
)
{
  public const string IndexerGetName = "[]";

  /// <summary>Differs from the getter's name so they're never overloads of each other.</summary>
  public const string IndexerSetName = "[]=";

  public string GetSignature()
  {
    switch (Indexer)
    {
      case WrenIndexer.Get:
        return "[" + Placeholders(Parameters.Count) + "]";

      case WrenIndexer.Set:
        return "[" + Placeholders(Parameters.Count - 1) + "]=(_)";
    }

    if (IsGetter)
      return WrenName;

    return WrenName + '(' + Placeholders(Parameters.Count) + ')';
  }

  public string WrenName => char.ToLowerInvariant(Name[0]) + Name.Substring(1);


  private static string Placeholders(int count) => string.Join(",", Enumerable.Repeat("_", count));
}

public enum WrenIndexer
{
  None,

  Get,

  Set,
}
