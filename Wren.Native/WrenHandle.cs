namespace Wren;

/// <summary>
/// Keeps a Wren value alive until disposed. No .NET finalizer: it must be disposed on the VM's thread.
/// </summary>
public sealed class WrenHandle : IDisposable
{
    private nint _ptr;

    internal WrenHandle(WrenVm vm, nint ptr)
    {
        Vm = vm;
        _ptr = ptr;
    }

    public WrenVm Vm { get; }

    public bool IsValid => _ptr != 0;

    public nint Ptr => _ptr != 0 ? _ptr : throw new ObjectDisposedException(nameof(WrenHandle));

    public void Dispose()
    {
        if (_ptr != 0)
            Vm.ReleaseHandle(this);
    }

    internal void Invalidate() => _ptr = 0;
}
