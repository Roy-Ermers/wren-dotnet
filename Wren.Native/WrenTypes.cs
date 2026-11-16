using Wren.Native;

namespace Wren;

/// <summary>A compile error, a runtime error, or one line of a runtime error's stack trace.</summary>
public readonly record struct WrenError(WrenErrorType Type, string? Module, int Line, string Message)
{
    public override string ToString() => Type switch
    {
        WrenErrorType.Compile => $"[{Module}:{Line}] Compile error: {Message}",
        WrenErrorType.StackTrace => $"  at {Message} ({Module}:{Line})",
        _ => $"Runtime error: {Message}",
    };
}

public sealed class WrenException(WrenInterpretResult result, IReadOnlyList<WrenError> errors)
  : Exception(BuildMessage(result, errors))
{
    public WrenInterpretResult Result { get; } = result;

    /// <summary>A compile error, or a runtime error followed by its stack trace.</summary>
    public IReadOnlyList<WrenError> Errors { get; } = errors;

    private static string BuildMessage(WrenInterpretResult result, IReadOnlyList<WrenError> errors)
    {
        var kind = result == WrenInterpretResult.CompileError ? "Wren compile error" : "Wren runtime error";
        return errors.Count == 0 ? kind + "." : kind + ":" + Environment.NewLine + string.Join(Environment.NewLine, errors);
    }
}

/// <summary>Receives the output of <c>System.print</c> and <c>System.write</c>.</summary>
public delegate void WrenWriteHandler(string text);

/// <summary>Receives every error as it's reported, in addition to the thrown <see cref="WrenException"/>.</summary>
public delegate void WrenErrorHandler(WrenError error);

/// <summary>Returns the source of <paramref name="module"/>, or null if it doesn't exist.</summary>
public delegate string? WrenLoadModuleHandler(string module);

/// <summary>
/// Called for every import of <paramref name="name"/> by <paramref name="importer"/>; returns the canonical module
/// name, or null to fail the import.
/// </summary>
public delegate string? WrenResolveModuleHandler(string importer, string name);

public sealed class WrenVmOptions
{
    public WrenWriteHandler? Write { get; init; }

    public WrenErrorHandler? Error { get; init; }

    /// <summary>Consulted after the bindings.</summary>
    public WrenLoadModuleHandler? LoadModule { get; init; }

    public WrenResolveModuleHandler? ResolveModule { get; init; }

    /// <summary>Bytes to allocate before the first garbage collection. Null uses Wren's default (10 MB).</summary>
    public nuint? InitialHeapSize { get; init; }

    /// <summary>Null uses Wren's default (1 MB).</summary>
    public nuint? MinHeapSize { get; init; }

    /// <summary>Null uses Wren's default (50).</summary>
    public int? HeapGrowthPercent { get; init; }
}

/// <summary>
/// Provides foreign classes, methods and module source to a <see cref="WrenVm"/>, e.g. the generated
/// <c>WrenBinding</c>. Bindings are consulted in the order added; the first non-empty answer wins.
/// </summary>
public interface IWrenBinding
{
    /// <summary>Returns a <c>delegate* unmanaged[Cdecl]&lt;nint, void&gt;</c>, or 0 if not provided.</summary>
    nint BindMethod(string module, string className, bool isStatic, string signature);

    WrenForeignClassMethods BindClass(string module, string className);

    string? GetModuleSource(string module);
}
