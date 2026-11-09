using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using Wren.Native;

namespace Wren;

// Every callback catches everything: an exception escaping an UnmanagedCallersOnly method terminates the process.
internal static unsafe class WrenVmCallbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static void Write(nint vm, byte* text)
    {
        try
        {
            WrenVm.FromNative(vm).Options.Write?.Invoke(Utf8StringMarshaller.ConvertToManaged(text) ?? "");
        }
        catch (Exception e)
        {
            Report("write", e);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static void Error(nint vm, WrenErrorType type, byte* module, int line, byte* message)
    {
        try
        {
            var error = new WrenError(type, Utf8StringMarshaller.ConvertToManaged(module), line,
                Utf8StringMarshaller.ConvertToManaged(message) ?? "");
            WrenVm.FromNative(vm).ReportError(error);
        }
        catch (Exception e)
        {
            Report("error", e);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static WrenLoadModuleResult LoadModule(nint vm, byte* name)
    {
        try
        {
            var source = WrenVm.FromNative(vm).GetModuleSource(Utf8StringMarshaller.ConvertToManaged(name)!);
            if (source is null)
                return default;

            return new WrenLoadModuleResult
            {
                Source = Utf8StringMarshaller.ConvertToUnmanaged(source),
                OnComplete = &LoadModuleComplete,
            };
        }
        catch (Exception e)
        {
            Report("loadModule", e);
            return default;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static void LoadModuleComplete(nint vm, byte* name, WrenLoadModuleResult result)
    {
        Utf8StringMarshaller.Free(result.Source);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static nint BindForeignMethod(nint vm, byte* module, byte* className, byte isStatic, byte* signature)
    {
        try
        {
            var moduleName = Utf8StringMarshaller.ConvertToManaged(module)!;
            var classNameString = Utf8StringMarshaller.ConvertToManaged(className)!;
            var signatureString = Utf8StringMarshaller.ConvertToManaged(signature)!;

            foreach (var binding in WrenVm.FromNative(vm).Bindings)
            {
                var method = binding.BindMethod(moduleName, classNameString, isStatic != 0, signatureString);
                if (method != 0)
                    return method;
            }
        }
        catch (Exception e)
        {
            Report("bindForeignMethod", e);
        }

        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    public static WrenForeignClassMethods BindForeignClass(nint vm, byte* module, byte* className)
    {
        try
        {
            var moduleName = Utf8StringMarshaller.ConvertToManaged(module)!;
            var classNameString = Utf8StringMarshaller.ConvertToManaged(className)!;

            foreach (var binding in WrenVm.FromNative(vm).Bindings)
            {
                var methods = binding.BindClass(moduleName, classNameString);
                // Generated classes have no allocator (their constructors are static methods), but need their finalizer.
                if (methods.Allocate != 0 || methods.Finalize != 0)
                    return methods;
            }
        }
        catch (Exception e)
        {
            Report("bindForeignClass", e);
        }

        return default;
    }

    private static void Report(string callback, Exception e)
      => Console.Error.WriteLine($"Unhandled exception in the Wren {callback} callback: {e}");
}
