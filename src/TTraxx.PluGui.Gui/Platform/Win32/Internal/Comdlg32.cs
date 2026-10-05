using System.Runtime.InteropServices;
using System.Text;

namespace TTraxx.PluGui.Gui.Platform.Win32.Internal;

/// <summary>comdlg32.dll's open-file dialog: modal, owned by the host's top-level window.</summary>
internal static partial class Comdlg32
{
    private const int OFN_NOCHANGEDIR = 0x00000008;
    private const int OFN_PATHMUSTEXIST = 0x00000800;
    private const int OFN_FILEMUSTEXIST = 0x00001000;
    private const int OFN_EXPLORER = 0x00080000;

    private const int MaxPath = 4096;

    [StructLayout(LayoutKind.Sequential)]
    private struct OpenFileName
    {
        public int StructSize;
        public nint Owner;
        public nint Instance;
        public nint Filter;
        public nint CustomFilter;
        public int MaxCustomFilter;
        public int FilterIndex;
        public nint File;
        public int MaxFile;
        public nint FileTitle;
        public int MaxFileTitle;
        public nint InitialDirectory;
        public nint Title;
        public int Flags;
        public short FileOffset;
        public short FileExtension;
        public nint DefaultExtension;
        public nint CustomData;
        public nint Hook;
        public nint TemplateName;
        public nint Reserved;
        public int ReservedInt;
        public int FlagsEx;
    }

    [LibraryImport("comdlg32.dll", EntryPoint = "GetOpenFileNameW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetOpenFileName(ref OpenFileName openFileName);

    /// <summary>The chosen file's path, or null if cancelled.</summary>
    internal static string? ShowOpen(nint owner, string title, IReadOnlyList<FileDialogFilter> filters)
    {
        // Each filter is "Name (*.a;*.b)\0*.a;*.b\0", then "All files" last, the list ending in an extra \0.
        var filter = new StringBuilder();
        foreach (var entry in filters)
        {
            var patterns = string.Join(';', entry.Extensions.Select(extension => "*." + extension));
            filter.Append(entry.Name).Append(" (").Append(patterns).Append(")\0").Append(patterns).Append('\0');
        }

        filter.Append("All files (*.*)\0*.*\0\0");

        var filterText = Marshal.StringToHGlobalUni(filter.ToString());
        var titleText = Marshal.StringToHGlobalUni(title);
        var file = Marshal.AllocHGlobal(MaxPath * sizeof(char));
        try
        {
            Marshal.WriteInt16(file, 0);
            var openFileName = new OpenFileName
            {
                StructSize = Marshal.SizeOf<OpenFileName>(),
                Owner = owner,
                Filter = filterText,
                FilterIndex = 1,
                File = file,
                MaxFile = MaxPath,
                Title = titleText,
                Flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR,
            };

            return GetOpenFileName(ref openFileName) ? Marshal.PtrToStringUni(file) : null;
        }
        finally
        {
            Marshal.FreeHGlobal(filterText);
            Marshal.FreeHGlobal(titleText);
            Marshal.FreeHGlobal(file);
        }
    }
}
