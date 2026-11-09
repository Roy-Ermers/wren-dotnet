using System.Runtime.InteropServices;

using Wren.Native;

namespace Wren;

/// <summary>A Wren virtual machine. Must only be used from one thread at a time.</summary>
public sealed unsafe class WrenVm : IDisposable
{
    private const int MaxArity = 16;

    private nint _ptr;
    private GCHandle _self;

    private readonly List<IWrenBinding> _bindings = [];
    private readonly HashSet<WrenHandle> _handles = [];
    private readonly WrenHandle?[] _callHandles = new WrenHandle?[MaxArity + 1];
    private readonly Dictionary<(string Module, string Class), WrenHandle> _classes = new();

    private List<WrenError>? _errors;

    public WrenVm(WrenVmOptions? options = null)
    {
        Options = options ?? new WrenVmOptions();
        _self = GCHandle.Alloc(this);

        WrenConfiguration config;
        WrenApi.InitConfiguration(&config);

        config.LoadModule = &WrenVmCallbacks.LoadModule;
        config.BindForeignMethod = &WrenVmCallbacks.BindForeignMethod;
        config.BindForeignClass = &WrenVmCallbacks.BindForeignClass;
        config.Write = &WrenVmCallbacks.Write;
        config.Error = &WrenVmCallbacks.Error;
        config.UserData = (void*)GCHandle.ToIntPtr(_self);

        if (Options.InitialHeapSize is { } initialHeapSize)
            config.InitialHeapSize = initialHeapSize;
        if (Options.MinHeapSize is { } minHeapSize)
            config.MinHeapSize = minHeapSize;
        if (Options.HeapGrowthPercent is { } heapGrowthPercent)
            config.HeapGrowthPercent = heapGrowthPercent;

        _ptr = WrenApi.NewVM(&config);
    }

    public WrenVmOptions Options { get; }

    public nint Ptr => _ptr != 0 ? _ptr : throw new ObjectDisposedException(nameof(WrenVm));

    public bool IsDisposed => _ptr == 0;

    internal IReadOnlyList<IWrenBinding> Bindings => _bindings;

    public static WrenVm FromNative(nint vm) => (WrenVm)GCHandle.FromIntPtr(WrenApi.GetUserData(vm)).Target!;

    /// <summary>Must be added before interpreting code that uses them: Wren binds foreign members when declared.</summary>
    public void AddBinding(IWrenBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        _bindings.Add(binding);
    }

    /// <exception cref="WrenException">The source failed to compile, or raised a runtime error.</exception>
    public void Interpret(string module, string source)
    {
        var outer = BeginRun();
        var result = WrenApi.Interpret(Ptr, module, source);
        EndRun(outer, result);
    }

    /// <summary>
    /// Calls <paramref name="method"/> on the receiver in slot 0 with the arguments in the following slots; the result
    /// is in slot 0 afterwards.
    /// </summary>
    /// <exception cref="WrenException">The call raised a runtime error.</exception>
    /// <exception cref="InvalidOperationException">Wren is executing a foreign method, which can't call into Wren.</exception>
    public void Call(WrenHandle method)
    {
        if (WrenRuntime.IsInForeignCall)
            throw new InvalidOperationException(
                "Wren can't be called into while it's executing a foreign method. " +
                "Store the value and call it after the foreign method returns.");

        var outer = BeginRun();
        var result = WrenApi.Call(Ptr, method.Ptr);
        EndRun(outer, result);
    }

    private List<WrenError>? BeginRun()
    {
        var outer = _errors;
        _errors = null;
        return outer;
    }

    private void EndRun(List<WrenError>? outer, WrenInterpretResult result)
    {
        var errors = _errors;
        _errors = outer;

        if (result != WrenInterpretResult.Success)
            throw new WrenException(result, (IReadOnlyList<WrenError>?)errors ?? []);
    }

    /// <summary>Makes a handle for a method signature such as <c>update(_,_)</c>, for <see cref="Call"/>.</summary>
    public WrenHandle MakeCallHandle(string signature) => Track(WrenApi.MakeCallHandle(Ptr, signature));

    public WrenHandle GetSlotHandle(int slot) => Track(WrenApi.GetSlotHandle(Ptr, slot));

    /// <summary>The cached handle for calling a <c>Fn</c> with <paramref name="arity"/> arguments.</summary>
    public WrenHandle GetFunctionCallHandle(int arity)
    {
        if ((uint)arity > MaxArity)
            throw new ArgumentOutOfRangeException(nameof(arity), arity, "Wren functions take at most 16 arguments.");

        if (_callHandles[arity] is { IsValid: true } handle)
            return handle;

        var signature = arity == 0 ? "call()" : "call(" + string.Join(",", Enumerable.Repeat("_", arity)) + ")";
        return _callHandles[arity] = MakeCallHandle(signature);
    }

    /// <exception cref="InvalidOperationException">The module isn't loaded, or doesn't define the class.</exception>
    public void LoadClass(string module, string className, int slot)
    {
        var ptr = Ptr;

        if (_classes.TryGetValue((module, className), out var handle))
        {
            WrenApi.SetSlotHandle(ptr, slot, handle.Ptr);
            return;
        }

        // wrenHasVariable asserts the module exists, so check that first.
        if (!WrenApi.HasModule(ptr, module))
            throw new InvalidOperationException(
                $"Can't use class '{className}': module '{module}' isn't loaded. Import it in Wren first.");

        if (!WrenApi.HasVariable(ptr, module, className))
            throw new InvalidOperationException($"Module '{module}' doesn't define class '{className}'.");

        WrenApi.GetVariable(ptr, module, className, slot);
        _classes.Add((module, className), GetSlotHandle(slot));
    }

    public void CollectGarbage() => WrenApi.CollectGarbage(Ptr);

    private WrenHandle Track(nint ptr)
    {
        var handle = new WrenHandle(this, ptr);
        _handles.Add(handle);
        return handle;
    }

    internal void ReleaseHandle(WrenHandle handle)
    {
        if (_handles.Remove(handle) && _ptr != 0)
            WrenApi.ReleaseHandle(_ptr, handle.Ptr);

        handle.Invalidate();
    }

    internal void ReportError(WrenError error)
    {
        (_errors ??= []).Add(error);
        Options.Error?.Invoke(error);
    }

    internal string? GetModuleSource(string module)
    {
        foreach (var binding in _bindings)
        {
            if (binding.GetModuleSource(module) is { } source)
                return source;
        }

        return Options.LoadModule?.Invoke(module);
    }

    public void Dispose()
    {
        if (_ptr == 0)
            return;

        foreach (var handle in _handles)
        {
            WrenApi.ReleaseHandle(_ptr, handle.Ptr);
            handle.Invalidate();
        }

        _handles.Clear();
        _classes.Clear();
        Array.Clear(_callHandles);

        WrenApi.FreeVM(_ptr);
        _ptr = 0;
        _self.Free();
    }
}
