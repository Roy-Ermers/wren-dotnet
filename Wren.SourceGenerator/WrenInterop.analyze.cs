using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Wren.SourceGenerator.Attributes;
using Wren.SourceGenerator.helpers;
using Wren.SourceGenerator.Models;

namespace Wren.SourceGenerator;

public partial class WrenInterop
{
  private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat;

  private static WrenClassModel Analyze(GeneratorAttributeSyntaxContext context)
  {
    var classSymbol = (INamedTypeSymbol)context.TargetSymbol;

    var analyzer = new ClassAnalyzer(classSymbol, new AttributeSymbols(context.SemanticModel.Compilation));
    return analyzer.Analyze(context);
  }

  private static WrenEnumModel AnalyzeEnum(GeneratorAttributeSyntaxContext context)
  {
    var enumSymbol = (INamedTypeSymbol)context.TargetSymbol;
    var attributes = new AttributeSymbols(context.SemanticModel.Compilation);
    var wrenType = ClassAnalyzer.ToForeignType(context.Attributes[0], enumSymbol);

    var members = new List<WrenEnumMember>();
    foreach (var field in enumSymbol.GetMembers().OfType<IFieldSymbol>())
    {
      if (!field.HasConstantValue)
        continue;

      var attribute = ClassAnalyzer.GetAttribute(field, attributes.Property);
      var name = (attribute is null ? null : ClassAnalyzer.NameArgument(attribute))
                 ?? WrenHelpers.ConvertNameToWrenStyle(field.Name);

      // Invariant culture, so the value is a valid Wren number literal.
      var value = Convert.ToString(field.ConstantValue, System.Globalization.CultureInfo.InvariantCulture)!;

      members.Add(new(name, value));
    }

    return new(
        wrenType.ClassName,
        wrenType.Module,
        enumSymbol.ToDisplayString(TypeFormat),
        members.ToEquatableArray(),
        EquatableArray<DiagnosticInfo>.Empty
    );
  }

  // Attributes are matched by symbol, not name, so a user's own same-named attribute doesn't match.
  private sealed class AttributeSymbols(Compilation compilation)
  {
    public readonly Compilation Compilation = compilation;
    public readonly INamedTypeSymbol? Class = Get(compilation, "WrenTypeAttribute");
    public readonly INamedTypeSymbol? Method = Get(compilation, "WrenMethodAttribute");
    public readonly INamedTypeSymbol? Property = Get(compilation, "WrenPropertyAttribute");
    public readonly INamedTypeSymbol? Constructor = Get(compilation, "WrenConstructorAttribute");

    private static INamedTypeSymbol? Get(Compilation compilation, string name)
      => compilation.GetTypeByMetadataName($"{Templates.Namespace}.{name}");
  }

  private sealed class ClassAnalyzer(INamedTypeSymbol classSymbol, AttributeSymbols attributes)
  {
    private readonly List<DiagnosticInfo> _diagnostics = [];

    private readonly HashSet<ITypeSymbol> _collectionsInProgress = new(SymbolEqualityComparer.Default);

    private readonly List<Binding> _bindings = [];

    // Same signature and Family ("method" or "construct") means overloads; a null Family can't be overloaded.
    private readonly record struct Binding(
        string Signature, bool IsStatic, string? Family, string OverloadKey, ISymbol Symbol);

    public WrenClassModel Analyze(GeneratorAttributeSyntaxContext context)
    {
      if (context.TargetNode is TypeDeclarationSyntax declaration &&
          !declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
      {
        _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.ClassMustBePartial,
            classSymbol.Locations.FirstOrDefault(), classSymbol.Name));
      }

      var containingTypes = GetContainingTypes();

      if (classSymbol.IsGenericType || containingTypes.Any(t => t.IsGenericType))
      {
        _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.GenericClassNotSupported,
            classSymbol.Locations.FirstOrDefault(), classSymbol.ToDisplayString()));
      }

      foreach (var containingType in containingTypes.Where(t => !IsPartial(t)))
      {
        _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.ContainingTypeMustBePartial,
            containingType.Locations.FirstOrDefault(), containingType.Name, classSymbol.Name));
      }

      // Same resolution as for foreign return values, so both always name the same Wren class.
      var foreignType = ToForeignType(context.Attributes[0], classSymbol);

      var constructors = GetConstructors();
      var (properties, members) = GetMembers();
      var methods = constructors.Concat(members).ToEquatableArray();
      CheckSignatures();

      string? ns = null;
      if (!classSymbol.ContainingNamespace.IsGlobalNamespace)
        ns = classSymbol.ContainingNamespace.ToDisplayString();

      return new(
          foreignType.ClassName,
          classSymbol.Name,
          foreignType.Module,
          ns,
          classSymbol.ToDisplayString(TypeFormat),
          containingTypes.Select(PartialDeclaration).ToEquatableArray(),
          methods,
          properties,
          _diagnostics.ToEquatableArray(),
          InheritanceDepth(),
          classSymbol.IsStatic,
          HasWrenTypeBase(),
          PartialDeclaration(classSymbol),
          classSymbol.IsValueType
      );
    }

    private int InheritanceDepth()
    {
      var depth = 0;
      for (var type = classSymbol.BaseType; type is not null && !IsRootType(type); type = type.BaseType)
        depth++;
      return depth;
    }

    private static bool IsRootType(ITypeSymbol type)
      => type.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType;

    private bool HasWrenTypeBase()
    {
      for (var type = classSymbol.BaseType; type is not null; type = type.BaseType)
      {
        if (HasAttribute(type, attributes.Class))
          return true;
      }

      return false;
    }

    private List<INamedTypeSymbol> GetContainingTypes()
    {
      var result = new List<INamedTypeSymbol>();
      for (var type = classSymbol.ContainingType; type is not null; type = type.ContainingType)
        result.Insert(0, type);
      return result;
    }

    private static bool IsPartial(INamedTypeSymbol type)
      => type.DeclaringSyntaxReferences
          .Select(reference => reference.GetSyntax())
          .OfType<TypeDeclarationSyntax>()
          .Any(declaration => declaration.Modifiers.Any(SyntaxKind.PartialKeyword));

    private static string PartialDeclaration(INamedTypeSymbol type)
    {
      var keyword = type switch
      {
        { IsRecord: true, TypeKind: TypeKind.Struct } => "record struct",
        { IsRecord: true } => "record",
        { TypeKind: TypeKind.Struct } => "struct",
        { TypeKind: TypeKind.Interface } => "interface",
        _ => "class"
      };

      return $"partial {keyword} {type.Name}";
    }

    #region Members

    // Bound as static `new` methods, not Wren `construct`: a foreign class has one allocator shared by all constructors,
    // and it can't use extra slots.
    private List<WrenMethod> GetConstructors()
    {
      var result = new List<WrenMethod>();
      foreach (var ctor in classSymbol.InstanceConstructors)
      {
        if (!HasAttribute(ctor, attributes.Constructor) && !IsAutoBoundPrimaryConstructor(ctor))
          continue;

        var parameters = ParseParameters(ctor.Parameters);
        var constructor = new WrenMethod(
            "new",
            ".ctor",
            IsStatic: true,
            IsOperator: false,
            classSymbol.ToDisplayString(TypeFormat),
            parameters,
            GetForeignType(classSymbol),
            ReturnKind: WrenValueKind.Foreign,
            IsConstructor: true);

        result.Add(constructor);
        _bindings.Add(new(constructor.GetSignature(), IsStatic: true, "construct", OverloadKey(parameters), ctor));
      }

      return result;
    }

    // A record's primary constructor is bound without [WrenConstructor]; as it wasn't asked for, it's skipped rather
    // than reported if a parameter can't be passed from Wren.
    private bool IsAutoBoundPrimaryConstructor(IMethodSymbol ctor)
    {
      if (!classSymbol.IsRecord)
        return false;

      var primary = ctor.DeclaringSyntaxReferences
          .Any(r => r.GetSyntax() is RecordDeclarationSyntax { ParameterList: not null });

      return primary && ctor.Parameters.All(p =>
          p.RefKind == RefKind.None && Classify(p.Type) != WrenValueKind.Other && CanReadFromWren(p.Type));
    }

    // WREN002: members that can't be overloads share a signature. WREN011: overloads Wren can't tell apart.
    private void CheckSignatures()
    {
      foreach (var group in _bindings.GroupBy(b => (b.Signature, b.IsStatic)))
      {
        var members = group.ToList();
        if (members.Count < 2)
          continue;

        var first = members[0];
        var canOverload = first.Family is not null && members.All(m => m.Family == first.Family);

        if (!canOverload)
        {
          foreach (var member in members.Skip(1))
          {
            _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.DuplicateWrenSignature,
                member.Symbol.Locations.FirstOrDefault(),
                classSymbol.Name, group.Key.Signature, Display(first.Symbol), Display(member.Symbol)));
          }

          continue;
        }

        foreach (var overloads in members.GroupBy(m => m.OverloadKey))
        {
          var called = overloads.First();
          foreach (var shadowed in overloads.Skip(1))
          {
            _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.AmbiguousOverloads,
                shadowed.Symbol.Locations.FirstOrDefault(),
                Display(called.Symbol), Display(shadowed.Symbol), group.Key.Signature,
                overloads.Key.Length == 0 ? "no arguments" : overloads.Key));
          }
        }
      }
    }

    private static string OverloadKey(IEnumerable<WrenParameter> parameters)
      => string.Join(",", parameters.Select(p =>
          p.Kind.ToWrenType()
          + (p.Nullable ? "?" : "")
          + (p.Kind is WrenValueKind.Foreign or WrenValueKind.Other ? $" {p.Type}" : "")));

    private static string Display(ISymbol symbol)
      => symbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);

    private (EquatableArray<WrenProperty> properties, EquatableArray<WrenMethod> methods) GetMembers()
    {
      var properties = new List<WrenProperty>();
      var methods = new List<WrenMethod>();

      // Overridden base members are skipped: the override is bound, and virtual dispatch reaches it anyway.
      var overridden = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

      // Wren foreign classes can't inherit, so inherited members are flattened into this class' binding.
      for (var type = classSymbol; type is not null && !IsRootType(type); type = type.BaseType)
      {
        var inherited = !SymbolEqualityComparer.Default.Equals(type, classSymbol);

        foreach (var member in type.GetMembers())
        {
          if (overridden.Contains(member))
            continue;

          // The generated code lives in this class, so only base members accessible from here can be bound.
          if (inherited && !attributes.Compilation.IsSymbolAccessibleWithin(member, classSymbol))
            continue;

          switch (member)
          {
            case IMethodSymbol { MethodKind: MethodKind.Ordinary } methodSymbol:
              {
                if (HasAttribute(methodSymbol, attributes.Constructor))
                {
                  // A base class' named constructors create the base class.
                  if (!inherited)
                    AddMethod(ParseNamedConstructor(methodSymbol), methodSymbol);
                  break;
                }

                AddMethod(ParseMethod(methodSymbol), methodSymbol);
                break;
              }
            case IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator } methodSymbol:
              AddMethod(ParseOperator(methodSymbol), methodSymbol);
              break;
            case IPropertySymbol { IsIndexer: true } indexerSymbol:
              foreach (var accessor in ParseIndexer(indexerSymbol))
                AddMethod(accessor, indexerSymbol);
              break;
            case IPropertySymbol propertySymbol:
              AddProperty(ParseProperty(propertySymbol), propertySymbol);
              break;
            case IFieldSymbol { IsImplicitlyDeclared: false } fieldSymbol:
              AddProperty(ParseField(fieldSymbol), fieldSymbol);
              break;
          }
        }
      }

      return (properties.ToEquatableArray(), methods.ToEquatableArray());

      void AddMethod(WrenMethod? method, ISymbol symbol)
      {
        if (method is null)
          return;

        methods.Add(method);
        _bindings.Add(new(method.GetSignature(), method.IsStatic, "method", OverloadKey(method.Parameters), symbol));
        AddOverridden(symbol);
      }

      void AddProperty(WrenProperty? property, ISymbol symbol)
      {
        if (property is null)
          return;

        properties.Add(property);
        AddOverridden(symbol);

        var name = WrenHelpers.ConvertNameToWrenStyle(property.Name);
        if (property.Getter)
          _bindings.Add(new(name, property.IsStatic, null, "", symbol));
        if (property.Setter)
          _bindings.Add(new($"{name}=(_)", property.IsStatic, null, "", symbol));
      }

      void AddOverridden(ISymbol symbol)
      {
        for (var current = Overridden(symbol); current is not null; current = Overridden(current))
          overridden.Add(current);
      }

      static ISymbol? Overridden(ISymbol symbol) => symbol switch
      {
        IMethodSymbol method => method.OverriddenMethod,
        IPropertySymbol property => property.OverriddenProperty,
        _ => null
      };
    }

    private WrenProperty? ParseProperty(IPropertySymbol symbol)
    {
      var attribute = GetAttribute(symbol, attributes.Property);
      if (attribute is null && !IsAutoBoundRecordProperty(symbol))
        return null;

      // A delegate can be set from a Wren Fn but not read back.
      var @delegate = GetDelegateInfo(symbol.Type, symbol.Locations.FirstOrDefault());

      var getter = symbol.GetMethod is not null && @delegate is null;
      var setter = symbol.SetMethod is { IsInitOnly: false };

      // A positional record struct property Wren can't set becomes read-only rather than reported: it wasn't asked for.
      if (attribute is null && !CanReadFromWren(symbol.Type))
        setter = false;

      CheckAccessorTypes(symbol, symbol.Type, getter, setter);

      return new(
          (attribute is null ? null : NameArgument(attribute)) ?? symbol.Name,
          symbol.Name,
          symbol.Type.ToDisplayString(TypeFormat),
          Classify(symbol.Type),
          getter,
          setter,
          symbol.IsStatic,
          GetForeignType(symbol.Type),
          @delegate,
          AcceptsNull(symbol.Type),
          GetCollection(symbol.Type)
      );
    }

    // Positional record properties are bound without [WrenProperty]; unsupported ones are skipped rather than reported.
    private bool IsAutoBoundRecordProperty(IPropertySymbol symbol)
    {
      if (!classSymbol.IsRecord || symbol.IsStatic || symbol.IsIndexer)
        return false;

      // A positional property is declared by its primary constructor parameter.
      var positional = symbol.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is ParameterSyntax);
      return positional && Classify(symbol.Type) is not (WrenValueKind.Other or WrenValueKind.Delegate);
    }

    private WrenProperty? ParseField(IFieldSymbol symbol)
    {
      var attribute = GetAttribute(symbol, attributes.Property);
      if (attribute is null)
        return null;

      var @delegate = GetDelegateInfo(symbol.Type, symbol.Locations.FirstOrDefault());

      var getter = @delegate is null;
      var setter = !symbol.IsReadOnly && !symbol.IsConst;
      CheckAccessorTypes(symbol, symbol.Type, getter, setter);

      return new(
          NameArgument(attribute) ?? symbol.Name,
          symbol.Name,
          symbol.Type.ToDisplayString(TypeFormat),
          Classify(symbol.Type),
          getter,
          setter,
          symbol.IsStatic,
          GetForeignType(symbol.Type),
          @delegate,
          AcceptsNull(symbol.Type),
          GetCollection(symbol.Type)
      );
    }

    private List<WrenMethod> ParseIndexer(IPropertySymbol symbol)
    {
      var result = new List<WrenMethod>();
      if (!HasAttribute(symbol, attributes.Property))
        return result;

      var type = symbol.Type.ToDisplayString(TypeFormat);
      var foreign = GetForeignType(symbol.Type);
      var @delegate = GetDelegateInfo(symbol.Type, symbol.Locations.FirstOrDefault());
      var indices = ParseParameters(symbol.Parameters);

      // A delegate can't be returned to Wren as a Fn.
      var getter = symbol.GetMethod is not null && @delegate is null;
      var setter = symbol.SetMethod is { IsInitOnly: false };
      CheckAccessorTypes(symbol, symbol.Type, getter, setter);

      if (getter)
      {
        result.Add(new(
            WrenMethod.IndexerGetName,
            "this[]",
            IsStatic: false,
            IsOperator: false,
            type,
            indices,
            foreign,
            Indexer: WrenIndexer.Get,
            ReturnKind: Classify(symbol.Type),
            ReturnNullable: AcceptsNull(symbol.Type),
            ReturnCollection: GetCollection(symbol.Type)
        ));
      }

      if (setter)
      {
        // Must differ from the index names in the generated Wren source.
        var valueName = indices.Any(p => WrenHelpers.ConvertNameToWrenStyle(p.Name) == "value") ? "newValue" : "value";
        var value = new WrenParameter(valueName, type, Classify(symbol.Type), @delegate, AcceptsNull(symbol.Type),
            GetCollection(symbol.Type));

        result.Add(new(
            WrenMethod.IndexerSetName,
            "this[]",
            IsStatic: false,
            IsOperator: false,
            "void",
            indices.Append(value).ToEquatableArray(),
            Indexer: WrenIndexer.Set
        ));
      }

      return result;
    }

    private WrenMethod? ParseNamedConstructor(IMethodSymbol symbol)
    {
      if (!symbol.IsStatic || !SymbolEqualityComparer.Default.Equals(symbol.ReturnType, classSymbol))
      {
        _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.InvalidNamedConstructor,
            symbol.Locations.FirstOrDefault(), symbol.Name, classSymbol.Name));
        return null;
      }

      if (!CheckParameterCount(symbol))
        return null;

      return new(
          symbol.Name,
          symbol.Name,
          IsStatic: true,
          IsOperator: false,
          symbol.ReturnType.ToDisplayString(TypeFormat),
          ParseParameters(symbol.Parameters),
          GetForeignType(symbol.ReturnType),
          ReturnKind: Classify(symbol.ReturnType),
          ReturnNullable: AcceptsNull(symbol.ReturnType),
          ReturnCollection: GetCollection(symbol.ReturnType)
      );
    }

    private WrenMethod? ParseMethod(IMethodSymbol symbol)
    {
      var attribute = GetAttribute(symbol, attributes.Method);
      if (attribute is null)
        return null;

      if (!CheckParameterCount(symbol))
        return null;

      var name = NameArgument(attribute) ?? symbol.Name;

      if (!symbol.ReturnsVoid)
        CheckReturnType(symbol, symbol.ReturnType);

      // String interpolation and System.print call the `toString` getter, so ToString() is bound as one.
      var isGetter = !symbol.IsStatic
                     && symbol.Parameters.Length == 0
                     && WrenHelpers.ConvertNameToWrenStyle(name) == "toString";

      return new(
          name,
          symbol.Name,
          symbol.IsStatic,
          IsOperator: false,
          symbol.ReturnsVoid ? "void" : symbol.ReturnType.ToDisplayString(TypeFormat),
          ParseParameters(symbol.Parameters),
          GetForeignType(symbol.ReturnType),
          isGetter,
          ReturnKind: Classify(symbol.ReturnType),
          ReturnNullable: AcceptsNull(symbol.ReturnType),
          ReturnCollection: GetCollection(symbol.ReturnType)
      );
    }

    private WrenMethod? ParseOperator(IMethodSymbol symbol)
    {
      if (!HasAttribute(symbol, attributes.Method))
        return null;

      var oper = symbol.Name switch
      {
        "op_UnaryNegation" => "-",
        "op_LogicalNot" => "!",
        "op_OnesComplement" => "~",

        "op_Addition" => "+",
        "op_Subtraction" => "-",
        "op_Multiply" => "*",
        "op_Division" => "/",
        "op_Modulus" => "%",
        "op_Equality" => "==",
        "op_Inequality" => "!=",
        "op_LessThan" => "<",
        "op_LessThanOrEqual" => "<=",
        "op_GreaterThan" => ">",
        "op_GreaterThanOrEqual" => ">=",
        "op_BitwiseAnd" => "&",
        "op_BitwiseOr" => "|",
        "op_ExclusiveOr" => "^",
        "op_LeftShift" => "<<",
        "op_RightShift" => ">>",
        _ => null
      };

      if (oper is null)
      {
        _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.UnsupportedOperator,
            symbol.Locations.FirstOrDefault(), symbol.Name));
        return null;
      }

      // The first operand is the Wren receiver.
      var parameters = ParseParameters(symbol.Parameters).Skip(1).ToEquatableArray();
      CheckReturnType(symbol, symbol.ReturnType);

      // Wren binds unary operators like getters: signature "-", separate from the binary "-(_)".
      var isUnary = parameters.Count == 0;

      return new(
          oper,
          "<unused>",
          IsStatic: false,
          IsOperator: true,
          symbol.ReturnType.ToDisplayString(TypeFormat),
          parameters,
          GetForeignType(symbol.ReturnType),
          IsGetter: isUnary,
          ReturnKind: Classify(symbol.ReturnType),
          ReturnNullable: AcceptsNull(symbol.ReturnType),
          ReturnCollection: GetCollection(symbol.ReturnType)
      );
    }

    private bool CheckParameterCount(IMethodSymbol symbol)
    {
      if (symbol.Parameters.Length <= 16)
        return true;

      _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.TooManyParameters,
          symbol.Locations.FirstOrDefault(), symbol.Name, symbol.Parameters.Length));
      return false;
    }

    #endregion

    #region Types

    private EquatableArray<WrenParameter> ParseParameters(ImmutableArray<IParameterSymbol> parameters)
    {
      var result = new List<WrenParameter>();
      foreach (var parameter in parameters)
      {
        // `in` is rejected too, to keep the Wren API independent of C# passing conventions.
        if (parameter.RefKind != RefKind.None)
        {
          _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.ByReferenceParameterNotSupported,
              parameter.Locations.FirstOrDefault(),
              parameter.Name,
              parameter.ContainingSymbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
              RefKindKeyword(parameter.RefKind)));
        }

        var @delegate = GetDelegateInfo(parameter.Type, parameter.Locations.FirstOrDefault());
        CheckParameterType(parameter.ContainingSymbol, parameter.Name, parameter.Type,
            parameter.Locations.FirstOrDefault());
        CheckReadable(parameter.ContainingSymbol, parameter.Type, parameter.Locations.FirstOrDefault());

        result.Add(new(
            parameter.Name,
            parameter.Type.ToDisplayString(TypeFormat),
            Classify(parameter.Type),
            @delegate,
            AcceptsNull(parameter.Type),
            GetCollection(parameter.Type)
        ));
      }

      return result.ToEquatableArray();
    }

    private void CheckParameterType(ISymbol member, string name, ITypeSymbol type, Location? location)
    {
      if (Classify(type) != WrenValueKind.Other)
        return;

      if (CollectionShape(type) is not null)
      {
        _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.UnsupportedType, location,
            Display(member), type.ToDisplayString()));
        return;
      }

      _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.SharedDataParameter, location,
          name, Display(member), type.ToDisplayString()));
    }

    private void CheckReturnType(ISymbol member, ITypeSymbol type)
    {
      if (Classify(type) is not (WrenValueKind.Other or WrenValueKind.Delegate))
        return;

      _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.UnsupportedType, member.Locations.FirstOrDefault(),
          Display(member), type.ToDisplayString()));
    }

    private void CheckAccessorTypes(ISymbol member, ITypeSymbol type, bool getter, bool setter)
    {
      if (getter)
        CheckReturnType(member, type);
      else if (setter)
        CheckParameterType(member, "value", type, member.Locations.FirstOrDefault());

      if (setter)
        CheckReadable(member, type, member.Locations.FirstOrDefault());
    }

    private static string RefKindKeyword(RefKind refKind) => refKind switch
    {
      RefKind.Out => "out",
      RefKind.Ref => "ref",
      RefKind.In => "in",
      RefKind.RefReadOnlyParameter => "ref readonly",
      _ => refKind.ToString().ToLowerInvariant()
    };

    // Nullable-oblivious reference types (#nullable disable) accept null, as C# allows it there.
    private static bool AcceptsNull(ITypeSymbol type)
    {
      if (IsNullableValueType(type, out _))
        return true;

      return type.IsReferenceType && type.NullableAnnotation != NullableAnnotation.NotAnnotated;
    }

    private static bool IsNullableValueType(ITypeSymbol type, out ITypeSymbol underlyingType)
    {
      if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
      {
        underlyingType = nullable.TypeArguments[0];
        return true;
      }

      underlyingType = type;
      return false;
    }

    private WrenValueKind Classify(ITypeSymbol type)
    {
      IsNullableValueType(type, out type);

      switch (type.SpecialType)
      {
        case SpecialType.System_Boolean: return WrenValueKind.Bool;
        case SpecialType.System_SByte:
        case SpecialType.System_Byte:
        case SpecialType.System_Int16:
        case SpecialType.System_UInt16:
        case SpecialType.System_Int32:
        case SpecialType.System_UInt32:
        case SpecialType.System_Int64:
        case SpecialType.System_UInt64:
        case SpecialType.System_IntPtr:
        case SpecialType.System_UIntPtr:
        case SpecialType.System_Decimal:
          return WrenValueKind.Number;
        case SpecialType.System_Single: return WrenValueKind.Float;
        case SpecialType.System_Double: return WrenValueKind.Double;
        case SpecialType.System_String: return WrenValueKind.String;
        case SpecialType.System_Char: return WrenValueKind.Char;
      }

      switch (type.TypeKind)
      {
        // With or without [WrenType], enums are passed as numbers.
        case TypeKind.Enum: return WrenValueKind.Enum;
        case TypeKind.Delegate: return WrenValueKind.Delegate;
      }

      // Before collections: a [WrenType] class implementing IEnumerable<T> stays foreign.
      if (GetForeignType(type) is not null)
        return WrenValueKind.Foreign;

      if (CollectionShape(type) is { } shape)
      {
        if (GetCollection(type) is null)
          return WrenValueKind.Other;

        return shape.Shape == WrenCollectionShape.Dictionary ? WrenValueKind.Map : WrenValueKind.List;
      }

      return WrenValueKind.Other;
    }

    private static (WrenCollectionShape Shape, ITypeSymbol Element, ITypeSymbol? Key)? CollectionShape(ITypeSymbol type)
    {
      if (type is IArrayTypeSymbol { Rank: 1 } array)
        return (WrenCollectionShape.Array, array.ElementType, null);

      if (type.SpecialType == SpecialType.System_String)
        return null;

      var types = new List<INamedTypeSymbol>(type.AllInterfaces);
      if (type is INamedTypeSymbol named)
        types.Add(named);

      var elements = types
          .Where(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
          .Select(i => i.TypeArguments[0])
          .Distinct<ITypeSymbol>(SymbolEqualityComparer.Default)
          .ToList();

      // Several element types would be ambiguous.
      if (elements.Count != 1)
        return null;

      var element = elements[0];
      if (element is INamedTypeSymbol { IsGenericType: true } pair &&
          pair.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.KeyValuePair<TKey, TValue>")
      {
        return (WrenCollectionShape.Dictionary, pair.TypeArguments[1], pair.TypeArguments[0]);
      }

      return (WrenCollectionShape.List, element, null);
    }

    private WrenCollection? GetCollection(ITypeSymbol type)
    {
      IsNullableValueType(type, out type);

      if (CollectionShape(type) is not { } shape)
        return null;

      // Guards against collections containing themselves (class Node : IEnumerable<Node>), which would recurse forever.
      if (!_collectionsInProgress.Add(type))
        return null;

      try
      {
        return GetCollection(type, shape);
      }
      finally
      {
        _collectionsInProgress.Remove(type);
      }
    }

    private WrenCollection? GetCollection(ITypeSymbol type,
        (WrenCollectionShape Shape, ITypeSymbol Element, ITypeSymbol? Key) shape)
    {
      if (ToValueType(shape.Element) is not { } element)
        return null;

      WrenValueType? key = null;
      if (shape.Key is not null)
      {
        key = ToValueType(shape.Key);
        if (key is not { Kind: WrenValueKind.Bool or WrenValueKind.Number or WrenValueKind.Float
                or WrenValueKind.Double or WrenValueKind.String or WrenValueKind.Char or WrenValueKind.Enum })
          return null;
      }

      var (canRead, constructedType) = GetReadInfo(type, shape);
      return new(shape.Shape, element, key, constructedType, canRead, type.IsValueType);
    }

    private (bool CanRead, string? ConstructedType) GetReadInfo(ITypeSymbol type,
        (WrenCollectionShape Shape, ITypeSymbol Element, ITypeSymbol? Key) shape)
    {
      if (shape.Shape == WrenCollectionShape.Array)
        return (true, null);

      var compilation = attributes.Compilation;
      var created = shape.Key is { } key
          ? compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2")?.Construct(key, shape.Element)
          : compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")?.Construct(shape.Element);

      if (created is null)
        return (false, null);

      if (Converts(created, type))
        return (true, null);

      var constructible = type is INamedTypeSymbol { IsAbstract: false, TypeKind: TypeKind.Class or TypeKind.Struct } named
                          && named.InstanceConstructors.Any(ctor =>
                              ctor.Parameters.Length == 1
                              && ctor.Parameters[0].RefKind == RefKind.None
                              && Converts(created, ctor.Parameters[0].Type)
                              && compilation.IsSymbolAccessibleWithin(ctor, classSymbol));

      return constructible
          ? (true, type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(TypeFormat))
          : (false, null);

      bool Converts(ITypeSymbol from, ITypeSymbol to) => compilation.ClassifyCommonConversion(from, to).IsImplicit;
    }

    private WrenValueType? ToValueType(ITypeSymbol type)
    {
      var kind = Classify(type);
      if (kind is WrenValueKind.Other or WrenValueKind.Delegate)
        return null;

      return new(type.ToDisplayString(TypeFormat), kind, GetForeignType(type), AcceptsNull(type), GetCollection(type));
    }

    // Nested maps can't be read: Wren's API can't list map keys, and the generated wrapper only passes keys of
    // top-level map arguments.
    private bool CanReadFromWren(ITypeSymbol type)
      => GetCollection(type) is not { } collection || collection.IsConstructible && !collection.HasNestedMap;

    private void CheckReadable(ISymbol member, ITypeSymbol type, Location? location)
    {
      if (CanReadFromWren(type))
        return;

      var descriptor = GetCollection(type) is { IsConstructible: false }
          ? Diagnostics.UnsupportedType
          : Diagnostics.UnsupportedMapArgument;

      _diagnostics.Add(DiagnosticInfo.Create(descriptor, location, Display(member), type.ToDisplayString()));
    }

    private WrenForeignType? GetForeignType(ITypeSymbol type)
    {
      IsNullableValueType(type, out type);

      if (type.TypeKind == TypeKind.Enum)
        return null;

      var attribute = GetAttribute(type, attributes.Class);
      return attribute is null ? null : ToForeignType(attribute, type);
    }

    private WrenDelegateInfo? GetDelegateInfo(ITypeSymbol type, Location? location)
    {
      if (type is not INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: { } invoke })
        return null;

      var delegateType = type.ToDisplayString(TypeFormat);
      var parameters = new List<WrenValueType>();

      foreach (var parameter in invoke.Parameters)
      {
        var value = GetCallbackValueType(parameter.Type);
        if (value is null || parameter.RefKind != RefKind.None)
        {
          _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.UnsupportedCallbackType, location,
              type.ToDisplayString(), parameter.Type.ToDisplayString()));
          return null;
        }

        parameters.Add(value);
      }

      WrenValueType? returnValue = null;
      if (!invoke.ReturnsVoid)
      {
        returnValue = GetCallbackValueType(invoke.ReturnType);
        if (returnValue is null || invoke.ReturnsByRef || invoke.ReturnsByRefReadonly)
        {
          _diagnostics.Add(DiagnosticInfo.Create(Diagnostics.UnsupportedCallbackType, location,
              type.ToDisplayString(), invoke.ReturnType.ToDisplayString()));
          return null;
        }
      }

      return new(delegateType, parameters.ToEquatableArray(), returnValue);
    }

    private WrenValueType? GetCallbackValueType(ITypeSymbol type)
    {
      var kind = Classify(type);
      var nullable = AcceptsNull(type);
      return kind switch
      {
        WrenValueKind.Other or WrenValueKind.Delegate or WrenValueKind.List or WrenValueKind.Map => null,
        WrenValueKind.Foreign => new(type.ToDisplayString(TypeFormat), kind, GetForeignType(type), nullable),
        _ => new(type.ToDisplayString(TypeFormat), kind, Nullable: nullable)
      };
    }

    #endregion

    #region Attributes

    internal static AttributeData? GetAttribute(ISymbol symbol, INamedTypeSymbol? attributeType)
    {
      if (attributeType is null)
        return null;

      foreach (var attribute in symbol.GetAttributes())
      {
        if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
          return attribute;
      }

      return null;
    }

    private static bool HasAttribute(ISymbol symbol, INamedTypeSymbol? attributeType)
      => GetAttribute(symbol, attributeType) is not null;

    internal static string? NameArgument(AttributeData attribute)
      => attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as string : null;

    internal static WrenForeignType ToForeignType(AttributeData classAttribute, ITypeSymbol type)
    {
      var arguments = classAttribute.ConstructorArguments;
      var module = arguments.Length > 0 ? arguments[0].Value as string : null;
      var className = arguments.Length > 1 ? arguments[1].Value as string : null;

      return new(module ?? "engine", className ?? type.Name, type.IsValueType);
    }

    #endregion
  }
}
