using System.Runtime.InteropServices;

namespace Wren.Native;

public enum WrenType
{
  Bool,
  Number,
  Foreign,
  List,
  Map,
  Null,
  String,

  /// <summary>Any other object, e.g. a <c>Fn</c>, a class or an instance of a non-foreign class.</summary>
  Unknown,
}

public enum WrenInterpretResult
{
  Success,
  CompileError,
  RuntimeError,
}

public enum WrenErrorType
{
  Compile,
  Runtime,
  StackTrace,
}

/// <summary>Initialize with <see cref="WrenApi.InitConfiguration"/>.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WrenConfiguration
{
  public delegate* unmanaged[Cdecl]<void*, nuint, void*, void*> Reallocate;

  /// <summary>The returned string is freed by Wren with <see cref="Reallocate"/>.</summary>
  public delegate* unmanaged[Cdecl]<nint, byte*, byte*, byte*> ResolveModule;

  public delegate* unmanaged[Cdecl]<nint, byte*, WrenLoadModuleResult> LoadModule;

  public delegate* unmanaged[Cdecl]<nint, byte*, byte*, byte, byte*, nint> BindForeignMethod;

  public delegate* unmanaged[Cdecl]<nint, byte*, byte*, WrenForeignClassMethods> BindForeignClass;

  public delegate* unmanaged[Cdecl]<nint, byte*, void> Write;

  public delegate* unmanaged[Cdecl]<nint, WrenErrorType, byte*, int, byte*, void> Error;

  public nuint InitialHeapSize;
  public nuint MinHeapSize;
  public int HeapGrowthPercent;
  public void* UserData;
}

[StructLayout(LayoutKind.Sequential)]
public struct WrenForeignClassMethods
{
  public nint Allocate;
  public nint Finalize;

  public WrenForeignClassMethods(nint allocate, nint finalize)
  {
    Allocate = allocate;
    Finalize = finalize;
  }
}

/// <summary><see cref="Source"/> must stay valid until <see cref="OnComplete"/> is called.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WrenLoadModuleResult
{
  public byte* Source;
  public delegate* unmanaged[Cdecl]<nint, byte*, WrenLoadModuleResult, void> OnComplete;
  public void* UserData;
}
