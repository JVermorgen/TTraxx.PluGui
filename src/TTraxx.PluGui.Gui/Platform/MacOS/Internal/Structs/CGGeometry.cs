using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.MacOS.Internal.Structs;

[StructLayout(LayoutKind.Sequential)]
internal struct CGPoint(double x, double y)
{
    public double X = x;
    public double Y = y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CGSize(double width, double height)
{
    public double Width = width;
    public double Height = height;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CGRect(double x, double y, double width, double height)
{
    public CGPoint Origin = new(x, y);
    public CGSize Size = new(width, height);
}

/// <summary>Second argument of objc_msgSendSuper — needed when our override of an NSView method still needs to call the standard implementation (e.g., setFrameSize:, see MacOsPlatformWindow).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct ObjCSuper
{
    public nint Receiver;
    public nint SuperClass;
}