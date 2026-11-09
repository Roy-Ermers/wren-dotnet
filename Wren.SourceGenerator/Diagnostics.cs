using Microsoft.CodeAnalysis;

namespace Wren.SourceGenerator;

    internal static class Diagnostics
    {
        private const string Category = "SourceGenerator";

        public static readonly DiagnosticDescriptor ClassMustBePartial = new(
            id: "WREN001",
            title: "WrenType class must be partial",
            messageFormat: "Type '{0}' is annotated with [WrenType] but is not declared 'partial'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
  
        public static readonly DiagnosticDescriptor DuplicateWrenSignature = new(
            id: "WREN002",
            title: "Duplicate Wren signature",
            messageFormat: "Type '{0}' has more than one member producing the Wren signature '{1}' ('{2}' and '{3}'). " +
                            "Give one of them a different Wren name, e.g. with [WrenMethod(\"name\")] or [WrenProperty(\"name\")].",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnsupportedType = new(
            id: "WREN010",
            title: "Type can't be passed between C# and Wren",
            messageFormat: "'{0}' uses type '{1}', which can't be passed between C# and Wren. Supported types are bool, numbers, " +
                           "char, string, enums, [WrenType] classes, lists and dictionaries of those, and, for " +
                           "parameters, delegates (as Wren functions).",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor AmbiguousOverloads = new(
            id: "WREN011",
            title: "Wren can't tell overloads apart",
            messageFormat: "'{0}' and '{1}' both bind the Wren signature '{2}' with the same argument types ({3}). " +
                           "Wren can't tell them apart, so '{0}' is always called.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor SharedDataParameter = new(
            id: "WREN012",
            title: "Parameter is read from a foreign object",
            messageFormat: "Parameter '{0}' of '{1}' has type '{2}', which isn't a [WrenType] class. It's read from a " +
                           "foreign object holding a GCHandle, which only works for objects created by other bindings.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnsafeCodeRequired = new(
            id: "WREN013",
            title: "Wren bindings require unsafe code",
            messageFormat: "The code generated for [WrenType] classes uses function pointers. Enable unsafe code " +
                           "with <AllowUnsafeBlocks>true</AllowUnsafeBlocks> in the project file.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor TooManyParameters = new(
            id: "WREN003",
            title: "Too many parameters for a Wren method",
            messageFormat: "Method '{0}' has {1} parameters, but Wren methods support a maximum of 16",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
        
        public static readonly DiagnosticDescriptor UnsupportedOperator = new(
            id: "WREN004",
            title: "This operator is not supported",
            messageFormat: "Operator '{0}' is not supported yet",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidNamedConstructor = new(
            id: "WREN005",
            title: "Invalid [WrenConstructor] method",
            messageFormat: "Method '{0}' is marked [WrenConstructor] but must be static and return '{1}'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor GenericClassNotSupported = new(
            id: "WREN007",
            title: "Generic [WrenType] types are not supported",
            messageFormat: "Type '{0}' is annotated with [WrenType] but is generic, or nested in a generic type. " +
                           "A Wren foreign class needs a single, closed C# type.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ContainingTypeMustBePartial = new(
            id: "WREN008",
            title: "Types containing a [WrenType] type must be partial",
            messageFormat: "Type '{0}' contains the [WrenType] type '{1}' but is not declared 'partial'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ByReferenceParameterNotSupported = new(
            id: "WREN009",
            title: "out/ref/in parameters are not supported",
            messageFormat: "Parameter '{0}' of '{1}' is declared '{2}', but Wren has no by-reference parameters. " +
                           "Return the value instead, e.g. a nullable result instead of a Try method with an out parameter.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnsupportedMapArgument = new(
            id: "WREN014",
            title: "Nested Wren maps can't be passed to C#",
            messageFormat: "'{0}' takes '{1}' from Wren, which contains a map in a list or map. Wren's API can't list a " +
                           "map's keys, so the generated Wren method passes them along, which only works for arguments " +
                           "that are maps themselves.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnsupportedCallbackType = new(
            id: "WREN006",
            title: "Unsupported type in Wren callback",
            messageFormat: "Delegate type '{0}' can't be used as a Wren callback: '{1}' is not supported. " +
                           "Callback parameters and return values must be bool, int, float, double, string or a [WrenType] type, " +
                           "and parameters can't be ref/out/in.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }