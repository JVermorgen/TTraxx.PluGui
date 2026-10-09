using SkiaSharp;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TTraxx.PluGui.Gui.Platform.MacOS.Internal;
using TTraxx.PluGui.Gui.Platform.MacOS.Internal.Constants;
using TTraxx.PluGui.Gui.Platform.MacOS.Internal.Structs;

namespace TTraxx.PluGui.Gui.Platform.MacOS;

/// <summary>
/// <para>
/// Cocoa equivalent of Win32PlatformWindow/LinuxPlatformWindow: a custom
/// NSView subclass ("PluginView"), embedded in the NSView* that the VST3 host
/// passes as parent, with a reusable Skia pixel buffer that is blitted as a
/// CGImage in -drawRect: every frame.
///</para>
///<para>
/// NOTE: this architecture has not yet been tested on real Mac hardware. The
/// architecture deliberately follows the same pattern as Win32/Linux (self-lookup
/// via an ivar instead of GWLP_USERDATA, class registration once in the
/// static ctor instead of Win32's RegisterClass), but the Objective-C runtime
/// layer itself (ObjC.cs) is the most error-prone part of these three
/// platform layers.
/// </para>
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1163:Unused parameter", Justification = "Suppress static callback warnings")]
internal sealed unsafe class MacOsPlatformWindow : IPlatformWindow
{
    private const string ViewClassName = "PluginView";

    // NSTrackingAreaOptions: MouseMoved | ActiveAlways | InVisibleRect
    private const nuint TrackingAreaOptions = 0x02 | 0x80 | 0x200;

    private static readonly nint s_nsViewClass;
    private static readonly nint s_viewClass;
    private static readonly nint s_managedIvar;

    private static readonly nint s_selAlloc;
    private static readonly nint s_selRelease;
    private static readonly nint s_selInitWithFrame;
    private static readonly nint s_selSetFrame;
    private static readonly nint s_selFrame;
    private static readonly nint s_selAddSubview;
    private static readonly nint s_selRemoveFromSuperview;
    private static readonly nint s_selSetNeedsDisplay;
    private static readonly nint s_selSetWantsLayer;
    private static readonly nint s_selWindow;
    private static readonly nint s_selBackingScaleFactor;
    private static readonly nint s_selMainScreen;
    private static readonly nint s_selCurrentContext;
    private static readonly nint s_selCGContext;
    private static readonly nint s_selLocationInWindow;
    private static readonly nint s_selConvertPointFromView;
    private static readonly nint s_selClickCount;
    private static readonly nint s_selDeltaY;
    private static readonly nint s_selModifierFlags;
    private static readonly nint s_selInitWithRectOptionsOwnerUserInfo;
    private static readonly nint s_selAddTrackingArea;
    private static readonly nint s_selRemoveTrackingArea;
    private static readonly nint s_selSetFrameSize;
    private static readonly nint s_selKeyDown;
    private static readonly nint s_selResignFirstResponder;
    private static readonly nint s_selMakeFirstResponder;

    private nint _view;
    private nint _trackingArea;
    private GCHandle _selfHandle;
    private IPlatformWindowHost? _host;

    private SKSurface? _skSurface;
    private byte[]? _pixelBuffer;
    private int _w;
    private int _h;

    private bool _isAttached;
    private bool _isDestroyed;

    // While a control edits text: the view then accepts being first responder, and takes the keys.
    private bool _wantsKeyboard;

    /// <summary>
    /// Registers the "PluginView" subclass of NSView once, with our own
    /// implementations for drawRect:/mouseDown:/...setFrameSize:.
    /// Same bootstrap timing as the static ctors of Win32PlatformWindow
    /// (native lib loading) and LinuxPlatformWindow (Skia resolver): runs
    /// guaranteed exactly once, before the first instance.
    /// </summary>
    static MacOsPlatformWindow()
    {
        // Same reason as on Linux: as an embedded plugin in a host DAW,
        // AppContext.BaseDirectory doesn't reliably point to our own
        // .vst3 bundle, so .NET's standard .dylib probing doesn't find
        // libSkiaSharp.dylib automatically. We look up our own binary
        // ourselves (via dladdr, see Darwin.cs) and load from there explicitly.
        Darwin.RegisterUIEngineResolver();

        s_nsViewClass = ObjC.GetClass("NSView");

        s_selAlloc = ObjC.Sel("alloc");
        s_selRelease = ObjC.Sel("release");
        s_selInitWithFrame = ObjC.Sel("initWithFrame:");
        s_selSetFrame = ObjC.Sel("setFrame:");
        s_selFrame = ObjC.Sel("frame");
        s_selAddSubview = ObjC.Sel("addSubview:");
        s_selRemoveFromSuperview = ObjC.Sel("removeFromSuperview");
        s_selSetNeedsDisplay = ObjC.Sel("setNeedsDisplay:");
        s_selSetWantsLayer = ObjC.Sel("setWantsLayer:");
        s_selWindow = ObjC.Sel("window");
        s_selBackingScaleFactor = ObjC.Sel("backingScaleFactor");
        s_selMainScreen = ObjC.Sel("mainScreen");
        s_selCurrentContext = ObjC.Sel("currentContext");
        s_selCGContext = ObjC.Sel("CGContext");
        s_selLocationInWindow = ObjC.Sel("locationInWindow");
        s_selConvertPointFromView = ObjC.Sel("convertPoint:fromView:");
        s_selClickCount = ObjC.Sel("clickCount");
        s_selDeltaY = ObjC.Sel("deltaY");
        s_selModifierFlags = ObjC.Sel("modifierFlags");
        s_selInitWithRectOptionsOwnerUserInfo = ObjC.Sel("initWithRect:options:owner:userInfo:");
        s_selAddTrackingArea = ObjC.Sel("addTrackingArea:");
        s_selRemoveTrackingArea = ObjC.Sel("removeTrackingArea:");
        s_selSetFrameSize = ObjC.Sel("setFrameSize:");
        s_selKeyDown = ObjC.Sel("keyDown:");
        s_selResignFirstResponder = ObjC.Sel("resignFirstResponder");
        s_selMakeFirstResponder = ObjC.Sel("makeFirstResponder:");

        s_viewClass = ObjC.AllocateClassPair(s_nsViewClass, ViewClassName, 0);

        // Pointer ivar that stores the GCHandle to the owner instance —
        // the Cocoa equivalent of Win32's GWLP_USERDATA/SetWindowUserData.
        _ = ObjC.AddIvar(s_viewClass, "_managed", (nuint)nint.Size, (byte)Math.Log2(nint.Size), "^v");

        _ = ObjC.AddMethod(s_viewClass, ObjC.Sel("isFlipped"), (nint)(delegate* unmanaged[Cdecl]<nint, nint, byte>)&IsFlippedImp, "c@:");
        _ = ObjC.AddMethod(s_viewClass, ObjC.Sel("drawRect:"), (nint)(delegate* unmanaged[Cdecl]<nint, nint, CGRect, void>)&DrawRectImp, "v@:{CGRect={CGPoint=dd}{CGSize=dd}}");
        _ = ObjC.AddMethod(s_viewClass, ObjC.Sel("mouseDown:"), (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&MouseDownImp, "v@:@");
        _ = ObjC.AddMethod(s_viewClass, ObjC.Sel("rightMouseDown:"), (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&RightMouseDownImp, "v@:@");
        _ = ObjC.AddMethod(s_viewClass, ObjC.Sel("mouseUp:"), (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&MouseUpImp, "v@:@");
        _ = ObjC.AddMethod(s_viewClass, ObjC.Sel("mouseDragged:"), (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&MouseMovedImp, "v@:@");
        _ = ObjC.AddMethod(s_viewClass, ObjC.Sel("mouseMoved:"), (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&MouseMovedImp, "v@:@");
        _ = ObjC.AddMethod(s_viewClass, ObjC.Sel("scrollWheel:"), (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&ScrollWheelImp, "v@:@");
        _ = ObjC.AddMethod(s_viewClass, s_selSetFrameSize, (nint)(delegate* unmanaged[Cdecl]<nint, nint, CGSize, void>)&SetFrameSizeImp, "v@:{CGSize=dd}");
        _ = ObjC.AddMethod(s_viewClass, ObjC.Sel("acceptsFirstResponder"), (nint)(delegate* unmanaged[Cdecl]<nint, nint, byte>)&AcceptsFirstResponderImp, "c@:");
        _ = ObjC.AddMethod(s_viewClass, s_selResignFirstResponder, (nint)(delegate* unmanaged[Cdecl]<nint, nint, byte>)&ResignFirstResponderImp, "c@:");
        _ = ObjC.AddMethod(s_viewClass, s_selKeyDown, (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, void>)&KeyDownImp, "v@:@");
        _ = ObjC.AddMethod(s_viewClass, ObjC.Sel("performKeyEquivalent:"), (nint)(delegate* unmanaged[Cdecl]<nint, nint, nint, byte>)&PerformKeyEquivalentImp, "c@:@");

        ObjC.RegisterClassPair(s_viewClass);

        s_managedIvar = ObjC.GetInstanceVariable(s_viewClass, "_managed");
    }

    public IEventPumpSource? EventPumpSource => null; // Cocoa's own run loop is already pumped by the host — no X11-like fd-polling needed

    public ITimerPumpSource? TimerPumpSource => null; // not implemented yet - see SetContinuousRepaint below

    public float GetInitialScaleFactor(nint parentHandle)
    {
        // The real backingScaleFactor comes from the NSWindow that contains
        // the parent view (1.0 normally, 2.0 on Retina). If the parent is not
        // yet in a window, fall back to the main screen.
        if (parentHandle != nint.Zero)
        {
            var window = ObjC.MsgSend(parentHandle, s_selWindow);
            if (window != nint.Zero) return (float)ObjC.MsgSendDouble(window, s_selBackingScaleFactor);
        }

        var mainScreen = ObjC.MsgSend(ObjC.GetClass("NSScreen"), s_selMainScreen);
        return mainScreen == nint.Zero ? 1.0f : (float)ObjC.MsgSendDouble(mainScreen, s_selBackingScaleFactor);
    }

    public bool Attach(nint parentHandle, int width, int height, IPlatformWindowHost host)
    {
        _host = host;
        if (parentHandle == nint.Zero) return false;

        var frame = new CGRect(0, 0, width, height);
        var alloc = ObjC.MsgSend(s_viewClass, s_selAlloc);
        _view = ObjC.MsgSendIdWithCGRect(alloc, s_selInitWithFrame, frame); // +1 retain, see Destroy() for the corresponding release
        if (_view == nint.Zero) return false;

        _selfHandle = GCHandle.Alloc(this);
        ObjC.SetIvar(_view, s_managedIvar, GCHandle.ToIntPtr(_selfHandle));

        ObjC.MsgSendVoidWithBool(_view, s_selSetWantsLayer, true);

        AddOrRefreshTrackingArea(frame);

        ObjC.MsgSendVoidWithIntPtr(parentHandle, s_selAddSubview, _view); // superview takes a +1 retain on it as well

        EnsureSurface(width, height);
        _isAttached = true;
        return true;
    }

    public void SetBounds(int x, int y, int width, int height)
    {
        if (!_isAttached || _isDestroyed) return;

        var frame = new CGRect(x, y, width, height);
        ObjC.MsgSendVoidWithCGRect(_view, s_selSetFrame, frame);
        EnsureSurface(width, height);
        AddOrRefreshTrackingArea(frame); // an NSTrackingArea doesn't automatically follow bounds — recreate on the new frame
    }

    public void Invalidate()
    {
        if (!_isAttached || _isDestroyed) return;
        ObjC.MsgSendVoidWithBool(_view, s_selSetNeedsDisplay, true);
    }

    /// <summary>
    /// No-op for now: a real implementation needs an NSTimer scheduled against
    /// this view (a new objc_msgSend shape mixing an NSTimeInterval double with
    /// pointer/BOOL args, calling back into a new registered selector) - untested,
    /// unverifiable raw-runtime code on a backend that's already unconfirmed on
    /// real hardware (see the type-level note above). A control that sets
    /// NeedsContinuousRepaint simply won't animate on macOS until this is wired up.
    /// </summary>
    public void SetContinuousRepaint(bool enabled) { }

    /// <summary>Dragging files out of the window needs NSDraggingSource (beginDraggingSession) - not done yet; declines, so a control offers another way out.</summary>
    public bool StartFileDrag(IReadOnlyList<string> paths) => false;

    /// <summary>
    /// Makes the view its window's first responder while a control edits text, so the keys come here
    /// rather than to the host (see CocoaKeyboard), and resigns it when the editing ends - to the
    /// window itself, not back to the host's previous responder, which may be gone by then. A click
    /// alone never takes it: the view accepts being first responder only while a control wants keys.
    /// </summary>
    public void SetKeyboardFocus(bool focused)
    {
        if (!_isAttached || _isDestroyed) return;

        var window = ObjC.MsgSend(_view, s_selWindow);
        if (window == nint.Zero) return;

        var isFirstResponder = ObjC.MsgSend(window, ObjC.Sel("firstResponder")) == _view;
        if (focused)
        {
            _wantsKeyboard = true;
            if (!isFirstResponder) _ = ObjC.MsgSendBoolWithIntPtr(window, s_selMakeFirstResponder, _view);
        }
        else
        {
            _wantsKeyboard = false;
            if (isFirstResponder) _ = ObjC.MsgSendBoolWithIntPtr(window, s_selMakeFirstResponder, nint.Zero);
        }
    }

    public string? GetClipboardText() => CocoaKeyboard.GetClipboardText();

    public void SetClipboardText(string text) => CocoaKeyboard.SetClipboardText(text);

    // For a host that passes keys on through VST3's onKeyDown instead: Shift makes a letter upper case.
    public string TranslateTypedCharacter(char character, KeyModifiers modifiers)
        => ((modifiers & KeyModifiers.Shift) != 0 ? char.ToUpperInvariant(character) : character).ToString();

    /// <summary>
    /// NSOpenPanel, run modally: one file, of the filters' extensions. Not verified on a Mac yet. (Files
    /// dropped from Finder aren't received: that needs the view to register for dragged types and
    /// implement the NSDraggingDestination methods - not done yet.)
    /// </summary>
    public string? ShowOpenFileDialog(string title, IReadOnlyList<FileDialogFilter> filters)
    {
        static nint NSString(string text) => ObjC.NSString(text);

        var panel = ObjC.MsgSend(ObjC.GetClass("NSOpenPanel"), ObjC.Sel("openPanel"));
        if (panel == nint.Zero) return null;

        ObjC.MsgSendVoidWithBool(panel, ObjC.Sel("setCanChooseFiles:"), true);
        ObjC.MsgSendVoidWithBool(panel, ObjC.Sel("setCanChooseDirectories:"), false);
        ObjC.MsgSendVoidWithBool(panel, ObjC.Sel("setAllowsMultipleSelection:"), false);
        ObjC.MsgSendVoidWithIntPtr(panel, ObjC.Sel("setMessage:"), NSString(title));

        var extensions = filters.SelectMany(filter => filter.Extensions).Distinct().Select(NSString).ToArray();
        if (extensions.Length > 0)
        {
            var handle = GCHandle.Alloc(extensions, GCHandleType.Pinned);
            try
            {
                var array = ObjC.MsgSendIdWithIntPtrNUInt(ObjC.GetClass("NSArray"), ObjC.Sel("arrayWithObjects:count:"),
                    handle.AddrOfPinnedObject(), (nuint)extensions.Length);
                ObjC.MsgSendVoidWithIntPtr(panel, ObjC.Sel("setAllowedFileTypes:"), array);
            }
            finally
            {
                handle.Free();
            }
        }

        const nint modalResponseOk = 1;
        if (ObjC.MsgSendNInt(panel, ObjC.Sel("runModal")) != modalResponseOk) return null;

        var url = ObjC.MsgSend(panel, ObjC.Sel("URL"));
        return url == nint.Zero ? null : ObjC.ToManagedString(ObjC.MsgSend(url, ObjC.Sel("path")));
    }

    public void Destroy()
    {
        if (_isDestroyed) return;

        // Resign first responder while the view is still in its window.
        SetKeyboardFocus(false);

        _isDestroyed = true;
        _isAttached = false;

        if (_trackingArea != nint.Zero)
        {
            ObjC.MsgSendVoidWithIntPtr(_view, s_selRemoveTrackingArea, _trackingArea);
            ObjC.MsgSendVoid(_trackingArea, s_selRelease);
            _trackingArea = nint.Zero;
        }

        if (_view != nint.Zero)
        {
            ObjC.MsgSendVoid(_view, s_selRemoveFromSuperview); // release superview's +1 retain
            ObjC.MsgSendVoid(_view, s_selRelease);              // release our own +1 retain (from alloc)
            _view = nint.Zero;
        }

        _skSurface?.Dispose();
        if (_selfHandle.IsAllocated) _selfHandle.Free();
    }

    #region "private"

    private void AddOrRefreshTrackingArea(CGRect frame)
    {
        if (_trackingArea != nint.Zero)
        {
            ObjC.MsgSendVoidWithIntPtr(_view, s_selRemoveTrackingArea, _trackingArea);
            ObjC.MsgSendVoid(_trackingArea, s_selRelease);
        }

        var alloc = ObjC.MsgSend(ObjC.GetClass("NSTrackingArea"), s_selAlloc);
        _trackingArea = ObjC.MsgSendIdTrackingArea(alloc, s_selInitWithRectOptionsOwnerUserInfo, frame, TrackingAreaOptions, _view, nint.Zero);
        ObjC.MsgSendVoidWithIntPtr(_view, s_selAddTrackingArea, _trackingArea);
    }

    private void EnsureSurface(int w, int h)
    {
        if (_skSurface != null && w == _w && h == _h) return;

        _w = w;
        _h = h;

        _pixelBuffer = new byte[w * h * 4];
        var pixels = GCHandle.Alloc(_pixelBuffer, GCHandleType.Pinned).AddrOfPinnedObject();

        var info = new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);
        _skSurface = SKSurface.Create(info, pixels, w * 4);
    }

    private void Repaint()
    {
        if (!_isAttached || _isDestroyed) return;
        if (_skSurface is null || _pixelBuffer is null) return;

        var canvas = _skSurface.Canvas;
        canvas.Save();
        canvas.Clear(SKColors.Transparent);
        _host?.OnPaint(canvas, _w, _h);
        canvas.Restore();
        _skSurface.Flush();

        var graphicsContext = ObjC.MsgSend(ObjC.GetClass("NSGraphicsContext"), s_selCurrentContext);
        if (graphicsContext == nint.Zero) return;
        var cgContext = ObjC.MsgSend(graphicsContext, s_selCGContext);
        if (cgContext == nint.Zero) return;

        fixed (byte* ptr = _pixelBuffer)
        {
            var colorSpace = CoreGraphics.CGColorSpaceCreateDeviceRGB();
            var provider = CoreGraphics.CGDataProviderCreateWithData(nint.Zero, ptr, (nuint)_pixelBuffer.Length, nint.Zero);
            var image = CoreGraphics.CGImageCreate((nuint)_w, (nuint)_h, 8, 32, (nuint)(_w * 4),
                colorSpace, CGConstants.BitmapInfoBgraPremultiplied, provider, nint.Zero, false, /* kCGRenderingIntentDefault */ 0);

            CoreGraphics.CGContextDrawImage(cgContext, new CGRect(0, 0, _w, _h), image);

            CoreGraphics.CGImageRelease(image);
            CoreGraphics.CGDataProviderRelease(provider);
            CoreGraphics.CGColorSpaceRelease(colorSpace);
        }
    }

    /// <summary>x/y van een NSEvent, omgezet naar view-lokale coördinaten. Dankzij isFlipped=YES (zie IsFlippedImp) is de oorsprong al linksboven, net als op Win32/X11 - geen handmatige flip nodig.</summary>
    private (int X, int Y) GetLocalPoint(nint eventPtr)
    {
        var windowPoint = ObjC.MsgSendCGPoint(eventPtr, s_selLocationInWindow);
        var localPoint = ObjC.MsgSendCGPointFromView(_view, s_selConvertPointFromView, windowPoint, nint.Zero);
        return ((int)localPoint.X, (int)localPoint.Y);
    }

    private void HandleMouseDown(nint eventPtr)
    {
        var (x, y) = GetLocalPoint(eventPtr);
        // Cocoa telt dubbelklikken al zelf (clickCount) - geen X11-achtige handmatige timing/afstand-logica nodig.
        if ((long)ObjC.MsgSendNInt(eventPtr, s_selClickCount) >= 2) _host?.OnDoubleClick(x, y);
        else _host?.OnPointerDown(x, y, GetModifiers(eventPtr));
    }

    private void HandleRightMouseDown(nint eventPtr)
    {
        var (x, y) = GetLocalPoint(eventPtr);
        _host?.OnContextMenu(x, y);
    }

    private void HandleMouseUp(nint eventPtr)
    {
        var (x, y) = GetLocalPoint(eventPtr);
        _host?.OnPointerUp(x, y);
    }

    private void HandleMouseMoved(nint eventPtr)
    {
        var (x, y) = GetLocalPoint(eventPtr);
        _host?.OnPointerMove(x, y, GetModifiers(eventPtr));
    }

    private static KeyModifiers GetModifiers(nint eventPtr)
    {
        var flags = ObjC.MsgSendNUInt(eventPtr, s_selModifierFlags);
        var modifiers = KeyModifiers.None;
        if ((flags & NSEventConstants.ModifierFlagShift) != 0) modifiers |= KeyModifiers.Shift;
        if ((flags & NSEventConstants.ModifierFlagControl) != 0) modifiers |= KeyModifiers.Control;
        return modifiers;
    }

    private void HandleScrollWheel(nint eventPtr)
    {
        var deltaY = ObjC.MsgSendDouble(eventPtr, s_selDeltaY);
        if (deltaY == 0) return;
        var (x, y) = GetLocalPoint(eventPtr);
        _host?.OnWheel(x, y, deltaY > 0 ? 1 : -1);
    }

    /// <summary>
    /// A key while the view has the keyboard: the key, then its text whatever became of the key - as
    /// Win32's WM_KEYDOWN and WM_CHAR; a text field takes a plain A as a key too, and still wants the
    /// "a". True when either was taken; false passes the key on.
    /// </summary>
    private bool HandleKeyDown(nint eventPtr)
    {
        if (!_wantsKeyboard || _host is null) return false;

        var (key, text) = CocoaKeyboard.Read(eventPtr);
        var keyTaken = key != Key.None && _host.OnKeyDown(new KeyEventArgs(key, CocoaKeyboard.ModifiersOf(eventPtr)));
        var textTaken = text is not null && _host.OnTextInput(text);
        return keyTaken || textTaken;
    }

    private static bool TryGetOwner(nint self, out MacOsPlatformWindow window)
    {
        var ptr = ObjC.GetIvar(self, s_managedIvar);
        window = (ptr != nint.Zero ? GCHandle.FromIntPtr(ptr).Target as MacOsPlatformWindow : null)!;
        return window != null;
    }

    // --- Native IMP-trampolines: statische callbacks die AppKit rechtstreeks aanroept, dispatchen naar de instance-methodes hierboven via de ivar-lookup ---

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static byte IsFlippedImp(nint self, nint _cmd) => 1; // YES: oorsprong linksboven, matcht Win32/X11

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void DrawRectImp(nint self, nint _cmd, CGRect dirtyRect)
    {
        if (TryGetOwner(self, out var window)) window.Repaint();
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void MouseDownImp(nint self, nint _cmd, nint evt)
    {
        if (TryGetOwner(self, out var window)) window.HandleMouseDown(evt);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void RightMouseDownImp(nint self, nint _cmd, nint evt)
    {
        if (TryGetOwner(self, out var window)) window.HandleRightMouseDown(evt);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void MouseUpImp(nint self, nint _cmd, nint evt)
    {
        if (TryGetOwner(self, out var window)) window.HandleMouseUp(evt);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void MouseMovedImp(nint self, nint _cmd, nint evt)
    {
        if (TryGetOwner(self, out var window)) window.HandleMouseMoved(evt);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void ScrollWheelImp(nint self, nint _cmd, nint evt)
    {
        if (TryGetOwner(self, out var window)) window.HandleScrollWheel(evt);
    }

    // Only while a control edits text: otherwise a click would make the view first responder and
    // take the host's keyboard shortcuts away.
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static byte AcceptsFirstResponderImp(nint self, nint _cmd)
        => TryGetOwner(self, out var window) && window._wantsKeyboard ? (byte)1 : (byte)0;

    // Something else became first responder (the host's own field, another plugin): the editing ends.
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static byte ResignFirstResponderImp(nint self, nint _cmd)
    {
        var super = new ObjCSuper { Receiver = self, SuperClass = s_nsViewClass };
        var resigned = ObjC.MsgSendSuperBool(ref super, s_selResignFirstResponder);
        if (resigned != 0 && TryGetOwner(self, out var window)) window._host?.OnKeyboardFocusLost();
        return resigned;
    }

    // A key no control takes goes on up the responder chain - to the host - as NSView's own keyDown: does.
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void KeyDownImp(nint self, nint _cmd, nint evt)
    {
        if (TryGetOwner(self, out var window) && window.HandleKeyDown(evt)) return;

        var super = new ObjCSuper { Receiver = self, SuperClass = s_nsViewClass };
        ObjC.MsgSendSuperVoidWithIntPtr(ref super, s_selKeyDown, evt);
    }

    // Cmd+key comes here first, through every view of the key window, before the host's menu could
    // take it: Cmd+C, V, X and A while a control edits text. Anything else: NO, as NSView answers.
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static byte PerformKeyEquivalentImp(nint self, nint _cmd, nint evt)
        => TryGetOwner(self, out var window) && window.HandleKeyDown(evt) ? (byte)1 : (byte)0;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void SetFrameSizeImp(nint self, nint _cmd, CGSize newSize)
    {
        // Eerst NSView's eigen implementatie laten lopen via objc_msgSendSuper
        // - die houdt de interne frame/bounds-ivars up-to-date. Zonder deze
        // call zouden latere aanroepen van 'frame'/'bounds' op deze view
        // stale blijven, want wij hebben setFrameSize: hier volledig
        // overschreven (niet aangevuld zoals een gewone C#-override).
        var super = new ObjCSuper { Receiver = self, SuperClass = s_nsViewClass };
        ObjC.MsgSendSuperVoidWithCGSize(ref super, s_selSetFrameSize, newSize);

        if (!TryGetOwner(self, out var window)) return;

        int w = (int)newSize.Width, h = (int)newSize.Height;
        if (w <= 0 || h <= 0) return;

        window.EnsureSurface(w, h);
        window._host?.OnResize(w, h);
    }

    #endregion
}
