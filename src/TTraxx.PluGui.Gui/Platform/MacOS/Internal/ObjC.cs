using System.Runtime.InteropServices;
using TTraxx.PluGui.Gui.Platform.MacOS.Internal.Structs;

namespace TTraxx.PluGui.Gui.Platform.MacOS.Internal;

/// <summary>
/// <para>
/// Minimal P/Invoke layer to the Objective-C runtime (libobjc), in the same
/// spirit as Xlib.cs for Linux: only the selectors that PluGui
/// actually uses, no generic bindings layer.
/// </para>
/// <para>
/// IMPORTANT — there is no "one size fits all" objc_msgSend: which
/// managed signature you use depends on the return type/arguments of the
/// specific selector, so below there is a separate overload for each
/// used combination (all pointing to the same native symbol via EntryPoint).
/// </para>
/// <para>
/// NOTE — CGRect (32 bytes) on x86_64 Mac needs a separate
/// objc_msgSend_stret call (Apple's own ABI quirk, resolved on
/// arm64 where plain objc_msgSend passes structs correctly) — see
/// GetCGRect() at the bottom. This is the most fragile spot in this file and
/// deserves an explicit test on an Intel Mac if that still needs to be supported.
/// </para>
/// </summary>
internal static partial class ObjC
{
    private const string ObjCLib = "/usr/lib/libobjc.A.dylib";

    // --- Class/selector lookup + class-pair registration (once, in the static ctor of MacOsPlatformWindow) ---

    [LibraryImport(ObjCLib, EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint GetClass(string name);

    [LibraryImport(ObjCLib, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint Sel(string name);

    [LibraryImport(ObjCLib, EntryPoint = "objc_allocateClassPair", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint AllocateClassPair(nint superclass, string name, nuint extraBytes);

    [LibraryImport(ObjCLib, EntryPoint = "objc_registerClassPair")]
    internal static partial void RegisterClassPair(nint cls);

    [LibraryImport(ObjCLib, EntryPoint = "class_addIvar", StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool AddIvar(nint cls, string name, nuint size, byte alignmentLog2, string types);

    [LibraryImport(ObjCLib, EntryPoint = "class_getInstanceVariable", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint GetInstanceVariable(nint cls, string name);

    // types-encoding below is deliberately approximate (e.g., "v@:@" for "void, id self, SEL _cmd, id arg").
    // Only relevant for introspection/KVO/NSInvocation, not for the direct dispatch path we use.
    [LibraryImport(ObjCLib, EntryPoint = "class_addMethod", StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool AddMethod(nint cls, nint selector, nint imp, string types);

    [LibraryImport(ObjCLib, EntryPoint = "object_setIvar")]
    internal static partial void SetIvar(nint obj, nint ivar, nint value);

    [LibraryImport(ObjCLib, EntryPoint = "object_getIvar")]
    internal static partial nint GetIvar(nint obj, nint ivar);

    // --- objc_msgSend variants, grouped by argument/return form ---

    [LibraryImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static partial nint MsgSend(nint receiver, nint selector); // alloc, window, mainScreen, currentContext, CGContext-property

    [LibraryImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static partial void MsgSendVoid(nint receiver, nint selector); // release, removeFromSuperview

    [LibraryImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static partial double MsgSendDouble(nint receiver, nint selector); // backingScaleFactor, deltaY

    [LibraryImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static partial nint MsgSendNInt(nint receiver, nint selector); // clickCount (NSInteger)

    [LibraryImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static partial nuint MsgSendNUInt(nint receiver, nint selector); // modifierFlags (NSEventModifierFlags/NSUInteger)

    [LibraryImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static partial nint MsgSendIdWithCGRect(nint receiver, nint selector, CGRect frame); // initWithFrame:

    [LibraryImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static partial void MsgSendVoidWithCGRect(nint receiver, nint selector, CGRect frame); // setFrame:

    [LibraryImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static partial void MsgSendVoidWithIntPtr(nint receiver, nint selector, nint arg); // addSubview:, addTrackingArea:, removeTrackingArea:

    [LibraryImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static partial void MsgSendVoidWithBool(nint receiver, nint selector, [MarshalAs(UnmanagedType.U1)] bool arg); // setNeedsDisplay:, setWantsLayer:

    [LibraryImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static partial CGPoint MsgSendCGPoint(nint receiver, nint selector); // locationInWindow (16 bytes - geen stret-probleem op geen van beide arch's)

    [LibraryImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static partial CGPoint MsgSendCGPointFromView(nint receiver, nint selector, CGPoint point, nint fromView); // convertPoint:fromView:

    [LibraryImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static partial nint MsgSendIdTrackingArea(nint receiver, nint selector, CGRect rect, nuint options, nint owner, nint userInfo); // [NSTrackingArea initWithRect:options:owner:userInfo:]

    [LibraryImport(ObjCLib, EntryPoint = "objc_msgSendSuper")]
    internal static partial void MsgSendSuperVoidWithCGSize(ref ObjCSuper super, nint selector, CGSize size); // super's setFrameSize: (see MacOsPlatformWindow.SetFrameSizeImp)

    // --- CGRect getter (e.g., -frame) with the x86_64/arm64 stret workaround ---

    [LibraryImport(ObjCLib, EntryPoint = "objc_msgSend")]
    private static partial CGRect MsgSendCGRectDirect(nint receiver, nint selector);

    [LibraryImport(ObjCLib, EntryPoint = "objc_msgSend_stret")]
    private static partial void MsgSendCGRectStret(out CGRect result, nint receiver, nint selector);

    private static readonly bool s_needsStret = RuntimeInformation.ProcessArchitecture == Architecture.X64;

    internal static CGRect GetCGRect(nint receiver, nint selector)
    {
        if (s_needsStret)
        {
            MsgSendCGRectStret(out var result, receiver, selector);
            return result;
        }
        return MsgSendCGRectDirect(receiver, selector);
    }
}