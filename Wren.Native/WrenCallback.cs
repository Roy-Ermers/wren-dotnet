using System.Runtime.CompilerServices;

using Wren.Native;

namespace Wren;

/// <summary>
/// Keeps a Wren function (<c>Fn</c>) alive so C# can call it through a delegate, until disposed or released with
/// <see cref="Release(Delegate)"/>. No .NET finalizer: handles must be released on the VM's thread.
/// </summary>
public sealed class WrenCallback : IDisposable
{
    private static readonly ConditionalWeakTable<Delegate, WrenCallback> s_Callbacks = new();

    private readonly WrenHandle _function;

    /// <summary>Takes ownership of <paramref name="function"/>.</summary>
    public WrenCallback(WrenHandle function, int paramCount)
    {
        _function = function ?? throw new ArgumentNullException(nameof(function));
        ParamCount = paramCount;
    }

    public static WrenCallback FromSlot(nint vm, int slot, int paramCount)
      => new(WrenVm.FromNative(vm).GetSlotHandle(slot), paramCount);

    public WrenVm Vm => _function.Vm;

    public int ParamCount { get; }

    public bool IsValid => _function.IsValid;

    /// <summary>
    /// Puts the function in slot 0 and returns the native VM; set the arguments in slots 1 to <see cref="ParamCount"/>,
    /// then call <see cref="Invoke"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Wren is executing a foreign method, which can't call into Wren.</exception>
    public nint Prepare(int slotCount)
    {
        if (!IsValid)
            throw new ObjectDisposedException(nameof(WrenCallback),
                "The Wren function has been released, or its VM has been disposed.");

        if (WrenRuntime.IsInForeignCall)
            throw new InvalidOperationException(
                "A Wren callback can't be invoked while Wren is executing a foreign method. " +
                "Store the callback and invoke it after the foreign method returns.");

        var vm = Vm.Ptr;
        WrenApi.EnsureSlots(vm, Math.Max(slotCount, ParamCount + 1));
        WrenApi.SetSlotHandle(vm, 0, _function.Ptr);
        return vm;
    }

    /// <summary>Calls the function prepared with <see cref="Prepare"/>; the result is in slot 0 afterwards.</summary>
    public void Invoke() => Vm.Call(Vm.GetFunctionCallHandle(ParamCount));

    /// <summary>Lets <see cref="Release(Delegate)"/> release this callback through <paramref name="callback"/>.</summary>
    public void Register(Delegate callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        s_Callbacks.AddOrUpdate(callback, this);
    }

    /// <summary>Releases the Wren function behind <paramref name="callback"/>, if it was created from one.</summary>
    public static bool Release(Delegate? callback)
    {
        if (callback is null || !s_Callbacks.TryGetValue(callback, out var wrenCallback))
            return false;

        s_Callbacks.Remove(callback);
        wrenCallback.Dispose();
        return true;
    }

    public void Dispose() => _function.Dispose();
}
