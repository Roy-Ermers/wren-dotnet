using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Wren.Native;
#pragma warning disable CA1401

/// <summary>Bindings of the upstream Wren 0.4 C API (<c>wren.h</c>).</summary>
public static unsafe partial class WrenApi
{
  public const string NativeLibrary = "wren";

  // [SuppressGCTransition] only on functions that can't run managed code: no allocation in Wren (it may call a
  // managed reallocator or run the GC and its managed finalizers), no callbacks, no blocking.

  #region VM

  [LibraryImport(NativeLibrary, EntryPoint = "wrenGetVersionNumber")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial int GetVersionNumber();

  [LibraryImport(NativeLibrary, EntryPoint = "wrenInitConfiguration")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void InitConfiguration(WrenConfiguration* configuration);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenNewVM")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial nint NewVM(WrenConfiguration* configuration);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenFreeVM")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void FreeVM(nint vm);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenCollectGarbage")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void CollectGarbage(nint vm);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenInterpret")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial WrenInterpretResult Interpret(nint vm, byte* module, byte* source);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenInterpret", StringMarshalling = StringMarshalling.Utf8)]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial WrenInterpretResult Interpret(nint vm, string module, string source);

  [SuppressGCTransition]
  [LibraryImport(NativeLibrary, EntryPoint = "wrenGetUserData")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial nint GetUserData(nint vm);

  [SuppressGCTransition]
  [LibraryImport(NativeLibrary, EntryPoint = "wrenSetUserData")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void SetUserData(nint vm, nint userData);

  #endregion

  #region Handles and calls

  [LibraryImport(NativeLibrary, EntryPoint = "wrenMakeCallHandle")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial nint MakeCallHandle(nint vm, byte* signature);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenMakeCallHandle", StringMarshalling = StringMarshalling.Utf8)]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial nint MakeCallHandle(nint vm, string signature);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenCall")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial WrenInterpretResult Call(nint vm, nint method);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenReleaseHandle")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void ReleaseHandle(nint vm, nint handle);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenAbortFiber")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  [SuppressGCTransition]
  public static partial void AbortFiber(nint vm, int slot);

  #endregion

  #region Slots

  [SuppressGCTransition]
  [LibraryImport(NativeLibrary, EntryPoint = "wrenGetSlotCount")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial int GetSlotCount(nint vm);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenEnsureSlots")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void EnsureSlots(nint vm, int numSlots);

  [SuppressGCTransition]
  [LibraryImport(NativeLibrary, EntryPoint = "wrenGetSlotType")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial WrenType GetSlotType(nint vm, int slot);

  [SuppressGCTransition]
  [LibraryImport(NativeLibrary, EntryPoint = "wrenGetSlotBool")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  [return: MarshalAs(UnmanagedType.U1)]
  public static partial bool GetSlotBool(nint vm, int slot);

  [SuppressGCTransition]
  [LibraryImport(NativeLibrary, EntryPoint = "wrenGetSlotBytes")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial byte* GetSlotBytes(nint vm, int slot, int* length);

  [SuppressGCTransition]
  [LibraryImport(NativeLibrary, EntryPoint = "wrenGetSlotDouble")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial double GetSlotDouble(nint vm, int slot);

  [SuppressGCTransition]
  [LibraryImport(NativeLibrary, EntryPoint = "wrenGetSlotForeign")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void* GetSlotForeign(nint vm, int slot);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenGetSlotString")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial byte* GetSlotString(nint vm, int slot);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenGetSlotHandle")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial nint GetSlotHandle(nint vm, int slot);

  [SuppressGCTransition]
  [LibraryImport(NativeLibrary, EntryPoint = "wrenSetSlotBool")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void SetSlotBool(nint vm, int slot, [MarshalAs(UnmanagedType.U1)] bool value);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenSetSlotBytes")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void SetSlotBytes(nint vm, int slot, byte* bytes, nuint length);

  [SuppressGCTransition]
  [LibraryImport(NativeLibrary, EntryPoint = "wrenSetSlotDouble")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void SetSlotDouble(nint vm, int slot, double value);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenSetSlotNewForeign")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void* SetSlotNewForeign(nint vm, int slot, int classSlot, nuint size);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenSetSlotNewList")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void SetSlotNewList(nint vm, int slot);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenSetSlotNewMap")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void SetSlotNewMap(nint vm, int slot);

  [SuppressGCTransition]
  [LibraryImport(NativeLibrary, EntryPoint = "wrenSetSlotNull")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void SetSlotNull(nint vm, int slot);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenSetSlotString")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void SetSlotString(nint vm, int slot, byte* text);

  [SuppressGCTransition]
  [LibraryImport(NativeLibrary, EntryPoint = "wrenSetSlotHandle")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void SetSlotHandle(nint vm, int slot, nint handle);

  #endregion

  #region Lists and maps

  [SuppressGCTransition]
  [LibraryImport(NativeLibrary, EntryPoint = "wrenGetListCount")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial int GetListCount(nint vm, int slot);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenGetListElement")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void GetListElement(nint vm, int listSlot, int index, int elementSlot);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenSetListElement")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void SetListElement(nint vm, int listSlot, int index, int elementSlot);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenInsertInList")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void InsertInList(nint vm, int listSlot, int index, int elementSlot);

  [SuppressGCTransition]
  [LibraryImport(NativeLibrary, EntryPoint = "wrenGetMapCount")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial int GetMapCount(nint vm, int slot);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenGetMapContainsKey")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  [return: MarshalAs(UnmanagedType.U1)]
  public static partial bool GetMapContainsKey(nint vm, int mapSlot, int keySlot);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenGetMapValue")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void GetMapValue(nint vm, int mapSlot, int keySlot, int valueSlot);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenSetMapValue")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void SetMapValue(nint vm, int mapSlot, int keySlot, int valueSlot);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenRemoveMapValue")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void RemoveMapValue(nint vm, int mapSlot, int keySlot, int removedValueSlot);

  #endregion

  #region Variables and modules

  [LibraryImport(NativeLibrary, EntryPoint = "wrenGetVariable")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void GetVariable(nint vm, byte* module, byte* name, int slot);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenGetVariable", StringMarshalling = StringMarshalling.Utf8)]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  public static partial void GetVariable(nint vm, string module, string name, int slot);

  /// <summary>Asserts (or crashes in release builds) if <paramref name="module"/> isn't loaded.</summary>
  [LibraryImport(NativeLibrary, EntryPoint = "wrenHasVariable")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  [return: MarshalAs(UnmanagedType.U1)]
  public static partial bool HasVariable(nint vm, byte* module, byte* name);

  /// <summary>Asserts (or crashes in release builds) if <paramref name="module"/> isn't loaded.</summary>
  [LibraryImport(NativeLibrary, EntryPoint = "wrenHasVariable", StringMarshalling = StringMarshalling.Utf8)]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  [return: MarshalAs(UnmanagedType.U1)]
  public static partial bool HasVariable(nint vm, string module, string name);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenHasModule")]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  [return: MarshalAs(UnmanagedType.U1)]
  public static partial bool HasModule(nint vm, byte* module);

  [LibraryImport(NativeLibrary, EntryPoint = "wrenHasModule", StringMarshalling = StringMarshalling.Utf8)]
  [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
  [return: MarshalAs(UnmanagedType.U1)]
  public static partial bool HasModule(nint vm, string module);

  #endregion
}

#pragma warning restore CA1401
