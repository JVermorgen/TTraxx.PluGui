using System.Runtime.InteropServices;
using TTraxx.PluGui.Gui.Platform.Win32.Internal.Structs;

namespace TTraxx.PluGui.Gui.Platform.Win32.Internal;

/// <summary>
/// shell32.dll bindings for files dropped from Explorer: the window opts in with DragAcceptFiles,
/// then each WM_DROPFILES carries an HDROP to query for the paths and the drop point, and to free.
/// </summary>
internal static partial class Shell32
{
    [LibraryImport("shell32.dll")]
    internal static partial void DragAcceptFiles(nint hWnd, [MarshalAs(UnmanagedType.Bool)] bool accept);

    /// <summary>With index 0xFFFFFFFF, the number of files; otherwise the path's length, or (with a buffer) the path itself.</summary>
    [LibraryImport("shell32.dll", EntryPoint = "DragQueryFileW")]
    internal static unsafe partial uint DragQueryFile(nint hDrop, uint index, char* buffer, uint bufferLength);

    [LibraryImport("shell32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DragQueryPoint(nint hDrop, out Point point);

    [LibraryImport("shell32.dll")]
    internal static partial void DragFinish(nint hDrop);

    /// <summary>Every path in a drop, and the point (client coordinates) it landed on. Frees the drop.</summary>
    internal static unsafe (List<string> Paths, Point Point) TakeDrop(nint hDrop)
    {
        var paths = new List<string>();
        try
        {
            var count = DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
            for (uint index = 0; index < count; index++)
            {
                var length = DragQueryFile(hDrop, index, null, 0);
                var buffer = new char[length + 1];
                fixed (char* pointer = buffer)
                {
                    DragQueryFile(hDrop, index, pointer, (uint)buffer.Length);
                }

                paths.Add(new string(buffer, 0, (int)length));
            }

            DragQueryPoint(hDrop, out var point);
            return (paths, point);
        }
        finally
        {
            DragFinish(hDrop);
        }
    }
}
