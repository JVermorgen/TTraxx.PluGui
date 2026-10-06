using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Win32.Internal;

/// <summary>
/// Drags files out of the plugin window, the way Explorer does: the shell builds the data object
/// for the files (CF_HDROP and the shell's own formats, so any target that takes a file from
/// Explorer takes these) and runs the drag with its default drop source and drag image
/// (SHDoDragDrop). Modal: it returns once the button is released or the drag is cancelled.
/// </summary>
internal static unsafe partial class FileDrag
{
    private const uint DROPEFFECT_COPY = 1;
    private const int DRAGDROP_S_DROP = 0x00040100;
    private const int CO_E_NOTINITIALIZED = unchecked((int)0x800401F0);
    private static readonly Guid IID_IDataObject = new("0000010e-0000-0000-C000-000000000046");

    /// <summary>
    /// True while a drag started here is under way - the window's own drop target refuses it, so a
    /// file dragged out can't land back in the window it came from.
    /// </summary>
    internal static bool IsDragging { get; private set; }

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHParseDisplayName(string name, nint bindContext, out nint pidl, uint attributesIn, out uint attributesOut);

    [LibraryImport("shell32.dll")]
    private static partial int SHCreateDataObject(nint folder, uint count, nint* children, nint inner, in Guid riid, out nint dataObject);

    [LibraryImport("shell32.dll")]
    private static partial int SHDoDragDrop(nint hwnd, nint dataObject, nint dropSource, uint okEffects, out uint effect);

    [LibraryImport("shell32.dll")]
    private static partial nint ILFindLastID(nint pidl);

    [LibraryImport("ole32.dll")]
    private static partial void CoTaskMemFree(nint memory);

    [LibraryImport("ole32.dll")]
    private static partial int OleInitialize(nint reserved);

    /// <summary>
    /// Drags <paramref name="paths"/> (existing files, all in one folder) from <paramref name="hwnd"/>;
    /// true when they were dropped somewhere that took them.
    /// </summary>
    internal static bool Start(nint hwnd, IReadOnlyList<string> paths)
    {
        if (paths.Count == 0) return false;

        var folder = Path.GetDirectoryName(paths[0]);
        if (folder is null) return false;

        // Each file's absolute item list; the data object wants the folder's and each file's last part.
        var items = new nint[paths.Count];
        nint folderItem = 0, dataObject = 0;
        try
        {
            if (SHParseDisplayName(folder, 0, out folderItem, 0, out _) < 0) return false;

            var children = stackalloc nint[paths.Count];
            for (var index = 0; index < paths.Count; index++)
            {
                if (!string.Equals(Path.GetDirectoryName(paths[index]), folder, StringComparison.OrdinalIgnoreCase)) return false;
                if (SHParseDisplayName(paths[index], 0, out items[index], 0, out _) < 0) return false;
                children[index] = ILFindLastID(items[index]);
            }

            if (SHCreateDataObject(folderItem, (uint)paths.Count, children, 0, IID_IDataObject, out dataObject) < 0) return false;

            IsDragging = true;
            var result = SHDoDragDrop(hwnd, dataObject, 0, DROPEFFECT_COPY, out var effect);
            if (result == CO_E_NOTINITIALIZED && OleInitialize(0) >= 0) result = SHDoDragDrop(hwnd, dataObject, 0, DROPEFFECT_COPY, out effect);
            return result == DRAGDROP_S_DROP && effect != 0;
        }
        finally
        {
            IsDragging = false;
            if (dataObject != 0) Release(dataObject);
            foreach (var item in items) if (item != 0) CoTaskMemFree(item);
            if (folderItem != 0) CoTaskMemFree(folderItem);
        }
    }

    /// <summary>IUnknown::Release, the third entry of every COM object's table.</summary>
    private static void Release(nint unknown) => ((delegate* unmanaged[Stdcall]<nint, uint>)(*(nint**)unknown)[2])(unknown);
}
