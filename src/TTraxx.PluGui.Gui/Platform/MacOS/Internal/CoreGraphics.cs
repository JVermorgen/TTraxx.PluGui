using System.Runtime.InteropServices;
using TTraxx.PluGui.Gui.Platform.MacOS.Internal.Structs;

namespace TTraxx.PluGui.Gui.Platform.MacOS.Internal;

/// <summary>
/// Direct C functions from CoreGraphics (no objc_msgSend needed —
/// this is plain C, so much less error-prone than ObjC.cs). Used to blit
/// the reused Skia pixel buffer every frame as a CGImage in the current
/// CGContext, analogous to XCreateImage/XPutImage on Linux.
/// </summary>
internal static partial class CoreGraphics
{
    private const string CGLib = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    [LibraryImport(CGLib)]
    internal static partial nint CGColorSpaceCreateDeviceRGB();

    [LibraryImport(CGLib)]
    internal static partial void CGColorSpaceRelease(nint colorSpace);

    [LibraryImport(CGLib)]
    internal static unsafe partial nint CGDataProviderCreateWithData(nint info, byte* data, nuint size, nint releaseCallback);

    [LibraryImport(CGLib)]
    internal static partial void CGDataProviderRelease(nint provider);

    [LibraryImport(CGLib)]
    internal static partial nint CGImageCreate(nuint width, nuint height, nuint bitsPerComponent, nuint bitsPerPixel,
        nuint bytesPerRow, nint colorSpace, uint bitmapInfo, nint provider, nint decode,
        [MarshalAs(UnmanagedType.U1)] bool shouldInterpolate, int intent);

    [LibraryImport(CGLib)]
    internal static partial void CGImageRelease(nint image);

    [LibraryImport(CGLib)]
    internal static partial void CGContextDrawImage(nint context, CGRect rect, nint image);
}