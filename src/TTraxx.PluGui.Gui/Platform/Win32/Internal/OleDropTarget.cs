using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TTraxx.PluGui.Gui.Platform.Win32.Internal.Structs;

namespace TTraxx.PluGui.Gui.Platform.Win32.Internal;

/// <summary>
/// <para>
/// An OLE IDropTarget for the plugin's window, so files dragged from Explorer land on it. Explorer
/// drags through OLE, and OLE gives the drag to the first window under the cursor - or up its
/// parents - with a registered drop target. A host that accepts drops itself (FL Studio does)
/// registers one on its own windows, so without one of ours the drag goes to the host, which
/// refuses it over a plugin. The older WM_DROPFILES route (DragAcceptFiles) is never reached then.
/// </para>
/// <para>
/// A hand-built COM object for NativeAOT: one block of native memory holding the vtable pointer, a
/// reference count and a GCHandle to the <see cref="Callbacks"/>; the vtable is static
/// UnmanagedCallersOnly functions. Lives from <see cref="Register"/> to <see cref="Revoke"/>.
/// </para>
/// <para>
/// Windows refuses drags from a normal process into an elevated one, so into a DAW run as
/// administrator a drop from Explorer never gets here.
/// </para>
/// </summary>
internal static unsafe partial class OleDropTarget
{
    /// <summary>What the window does with a drag: whether files may land at a point (client coordinates), and the drop.</summary>
    internal sealed class Callbacks(Func<int, int, bool> canDrop, Action<int, int, IReadOnlyList<string>> drop, nint hwnd)
    {
        public Func<int, int, bool> CanDrop { get; } = canDrop;
        public Action<int, int, IReadOnlyList<string>> Drop { get; } = drop;
        public nint Hwnd { get; } = hwnd;

        /// <summary>Whether the drag under way carries files.</summary>
        public bool HasFiles { get; set; }

        /// <summary>Whether this thread's OLE was started by us, so <see cref="Revoke"/> stops it again.</summary>
        public bool InitializedOle { get; set; }
    }

    private const int S_OK = 0;
    private const int E_NOINTERFACE = unchecked((int)0x80004002);
    private const int CO_E_NOTINITIALIZED = unchecked((int)0x800401F0);

    // What RegisterDragDrop actually returns on a thread whose OLE was never started.
    private const int E_OUTOFMEMORY = unchecked((int)0x8007000E);
    private const uint DROPEFFECT_NONE = 0;
    private const uint DROPEFFECT_COPY = 1;
    private const ushort CF_HDROP = 15;
    private const uint DVASPECT_CONTENT = 1;
    private const uint TYMED_HGLOBAL = 1;

    private static readonly Guid IidUnknown = new("00000000-0000-0000-C000-000000000046");
    private static readonly Guid IidDropTarget = new("00000122-0000-0000-C000-000000000046");

    [StructLayout(LayoutKind.Sequential)]
    private struct Instance
    {
        public nint* Vtable;
        public int References;
        public nint Callbacks; // GCHandle
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FormatEtc
    {
        public ushort Format;
        public nint TargetDevice;
        public uint Aspect;
        public int Index;
        public uint Medium;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct StgMedium
    {
        public uint Medium;
        public nint Handle;
        public nint UnknownForRelease;
    }

    /// <summary>A point in screen coordinates, as OLE passes it - by value, in one 64-bit register.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct PointL
    {
        public int X;
        public int Y;
    }

    [LibraryImport("ole32.dll")]
    private static partial int RegisterDragDrop(nint hwnd, nint dropTarget);

    [LibraryImport("ole32.dll")]
    private static partial int RevokeDragDrop(nint hwnd);

    [LibraryImport("ole32.dll")]
    private static partial int OleInitialize(nint reserved);

    [LibraryImport("ole32.dll")]
    private static partial void OleUninitialize();

    [LibraryImport("ole32.dll")]
    private static partial void ReleaseStgMedium(StgMedium* medium);

    private static readonly nint* Vtable = CreateVtable();

    /// <summary>Registers a drop target on <paramref name="callbacks"/>' window; the object to pass to <see cref="Revoke"/>, or zero if OLE refused.</summary>
    internal static nint Register(Callbacks callbacks)
    {
        var instance = (Instance*)NativeMemory.AllocZeroed((nuint)sizeof(Instance));
        instance->Vtable = Vtable;
        instance->References = 1;
        instance->Callbacks = GCHandle.ToIntPtr(GCHandle.Alloc(callbacks));

        var result = RegisterDragDrop(callbacks.Hwnd, (nint)instance);
        if (result is CO_E_NOTINITIALIZED or E_OUTOFMEMORY && OleInitialize(0) >= 0)
        {
            // A host that never started OLE on its UI thread (rare): start it for the window's lifetime.
            // That only works on a single-threaded apartment - a DAW's UI thread is one.
            callbacks.InitializedOle = true;
            result = RegisterDragDrop(callbacks.Hwnd, (nint)instance);
        }

        if (result == S_OK) return (nint)instance;

        Free(instance);
        return 0;
    }

    /// <summary>Unregisters the drop target and frees it.</summary>
    internal static void Revoke(nint target)
    {
        if (target == 0) return;

        var instance = (Instance*)target;
        var callbacks = CallbacksOf(instance);
        RevokeDragDrop(callbacks.Hwnd);
        if (callbacks.InitializedOle) OleUninitialize();
        Free(instance);
    }

    private static void Free(Instance* instance)
    {
        GCHandle.FromIntPtr(instance->Callbacks).Free();
        NativeMemory.Free(instance);
    }

    private static Callbacks CallbacksOf(Instance* instance) => (Callbacks)GCHandle.FromIntPtr(instance->Callbacks).Target!;

    private static nint* CreateVtable()
    {
        var vtable = (nint*)NativeMemory.Alloc(7, (nuint)sizeof(nint));
        vtable[0] = (nint)(delegate* unmanaged[Stdcall]<Instance*, Guid*, nint*, int>)&QueryInterface;
        vtable[1] = (nint)(delegate* unmanaged[Stdcall]<Instance*, uint>)&AddRef;
        vtable[2] = (nint)(delegate* unmanaged[Stdcall]<Instance*, uint>)&Release;
        vtable[3] = (nint)(delegate* unmanaged[Stdcall]<Instance*, nint, uint, PointL, uint*, int>)&DragEnter;
        vtable[4] = (nint)(delegate* unmanaged[Stdcall]<Instance*, uint, PointL, uint*, int>)&DragOver;
        vtable[5] = (nint)(delegate* unmanaged[Stdcall]<Instance*, int>)&DragLeave;
        vtable[6] = (nint)(delegate* unmanaged[Stdcall]<Instance*, nint, uint, PointL, uint*, int>)&Drop;
        return vtable;
    }

    // ---------------------------------------------------------------- IUnknown

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int QueryInterface(Instance* self, Guid* iid, nint* result)
    {
        if (*iid == IidUnknown || *iid == IidDropTarget)
        {
            *result = (nint)self;
            Interlocked.Increment(ref self->References);
            return S_OK;
        }

        *result = 0;
        return E_NOINTERFACE;
    }

    // The block is freed by Revoke, after OLE has dropped its references; the count is kept for COM's sake.
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint AddRef(Instance* self) => (uint)Interlocked.Increment(ref self->References);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint Release(Instance* self) => (uint)Interlocked.Decrement(ref self->References);

    // ---------------------------------------------------------------- IDropTarget

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int DragEnter(Instance* self, nint dataObject, uint keyState, PointL point, uint* effect)
    {
        var callbacks = CallbacksOf(self);
        var format = HDropFormat();
        callbacks.HasFiles = QueryGetData(dataObject, &format) == S_OK;
        *effect = EffectAt(callbacks, point, *effect);
        return S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int DragOver(Instance* self, uint keyState, PointL point, uint* effect)
    {
        *effect = EffectAt(CallbacksOf(self), point, *effect);
        return S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int DragLeave(Instance* self)
    {
        CallbacksOf(self).HasFiles = false;
        return S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int Drop(Instance* self, nint dataObject, uint keyState, PointL point, uint* effect)
    {
        var callbacks = CallbacksOf(self);
        *effect = EffectAt(callbacks, point, *effect);
        callbacks.HasFiles = false;
        if (*effect == DROPEFFECT_NONE) return S_OK;

        var format = HDropFormat();
        StgMedium medium;
        if (GetData(dataObject, &format, &medium) != S_OK) return S_OK;

        try
        {
            var paths = Shell32.ReadDropPaths(medium.Handle);
            var client = ToClient(callbacks.Hwnd, point);
            try
            {
                if (paths.Count > 0) callbacks.Drop(client.X, client.Y, paths);
            }
            catch
            {
                // An exception mustn't cross into the host's drag loop.
            }
        }
        finally
        {
            ReleaseStgMedium(&medium);
        }

        return S_OK;
    }

    /// <summary>Copy where the window takes files and the source allows copying; otherwise the "not allowed" cursor.</summary>
    private static uint EffectAt(Callbacks callbacks, PointL point, uint allowed)
    {
        if (!callbacks.HasFiles || (allowed & DROPEFFECT_COPY) == 0) return DROPEFFECT_NONE;

        var client = ToClient(callbacks.Hwnd, point);
        try
        {
            return callbacks.CanDrop(client.X, client.Y) ? DROPEFFECT_COPY : DROPEFFECT_NONE;
        }
        catch
        {
            return DROPEFFECT_NONE;
        }
    }

    private static Point ToClient(nint hwnd, PointL screen)
    {
        var point = new Point { X = screen.X, Y = screen.Y };
        User32.ScreenToClient(hwnd, ref point);
        return point;
    }

    private static FormatEtc HDropFormat() => new() { Format = CF_HDROP, Aspect = DVASPECT_CONTENT, Index = -1, Medium = TYMED_HGLOBAL };

    // IDataObject's vtable: QueryInterface, AddRef, Release, GetData (3), GetDataHere, QueryGetData (5), ...
    private static int GetData(nint dataObject, FormatEtc* format, StgMedium* medium)
        => ((delegate* unmanaged[Stdcall]<nint, FormatEtc*, StgMedium*, int>)(*(nint**)dataObject)[3])(dataObject, format, medium);

    private static int QueryGetData(nint dataObject, FormatEtc* format)
        => ((delegate* unmanaged[Stdcall]<nint, FormatEtc*, int>)(*(nint**)dataObject)[5])(dataObject, format);
}
