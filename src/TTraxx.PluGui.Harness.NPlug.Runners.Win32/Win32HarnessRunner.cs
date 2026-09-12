using NPlug;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TTraxx.PluGui.Gui.Helpers;
using TTraxx.PluGui.Harness.NPlug.Core.Helpers;
using TTraxx.PluGui.Harness.NPlug.Core.Interfaces;
using TTraxx.PluGui.NPlug.Interfaces;

namespace TTraxx.PluGui.Harness.NPlug.Runners.Win32;

/// <summary>
/// Direct Win32 P/Invoke harness runner.
/// </summary>
public sealed class Win32HarnessRunner : IHarnessRunner
{
    private const uint WindowStyle = 0x00C00000 | 0x00080000; // WS_CAPTION | WS_SYSMENU: fixed size, no resize border/min/max
    private const uint WS_EX_TOPMOST = 0x00000008;
    private const int CW_USEDEFAULT = unchecked((int)0x80000000);
    private const int SW_SHOW = 5;
    private const uint WM_DESTROY = 0x0002;
    private const uint SWP_NOMOVE = 0x0002, SWP_NOZORDER = 0x0004;
    private const int IDC_ARROW = 32512;
    private const int DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;

    private const uint WM_TIMER = 0x0113;
    private const int RefreshTimerId = 1;
    private const uint RefreshIntervalMs = 250; // ~4x/sec: fast enough to see a save immediately, light enough to leave the CPU alone

    private static Action? _refreshAction;
    private static Action? _rebuildControlsAction;

    private static IPluGuiPluginView? _typedView;

    public AudioPluginViewPlatform Platform => AudioPluginViewPlatform.Hwnd;

    public void Run(IHarnessPlugin plugin)
    {
        SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        Globals.ScaleFactor = 1.0f; // safe baseline; Attached() immediately detects the actual scale

        var view = plugin.Create();
        var initialSize = view.Size;
        (var windowWidth, var windowHeight) = ToWindowSize(initialSize.Right - initialSize.Left, initialSize.Bottom - initialSize.Top);

        var hInstance = GetModuleHandle(null);
        var className = $"PluGuiHarness_{Guid.NewGuid():N}";

        WNDCLASSEXW wc = new()
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            lpfnWndProc = GetStaticWndProcPointer(),
            hInstance = hInstance,
            hCursor = LoadCursorW(nint.Zero, IDC_ARROW),
            lpszClassName = className
        };
        if (RegisterClassExW(ref wc) == 0) throw new InvalidOperationException("RegisterClassExW failed.");

        var exStyle = plugin.AlwaysOnTop ? WS_EX_TOPMOST : 0u;
        var hwnd = CreateWindowExW(exStyle, className, plugin.DisplayName, WindowStyle,
            CW_USEDEFAULT, CW_USEDEFAULT, windowWidth, windowHeight, nint.Zero, nint.Zero, hInstance, nint.Zero);
        if (hwnd == nint.Zero) throw new InvalidOperationException("CreateWindowExW failed.");

        view.SetFrame(new Win32HarnessPluginFrame(hwnd));
        view.Attached(hwnd, plugin.Platform);

        _typedView = view as IPluGuiPluginView;

        if (_typedView is null)
        {
            _refreshAction = view.TryCreateDelegate("RefreshUI");
            _rebuildControlsAction = view.TryCreateDelegate("RebuildControls");
        }
        if (_typedView is not null || _refreshAction != null || _rebuildControlsAction != null)
            SetTimer(hwnd, RefreshTimerId, RefreshIntervalMs, nint.Zero);

        _ = ShowWindow(hwnd, SW_SHOW);

        while (GetMessageW(out var msg, nint.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessageW(ref msg);
        }

        if (_typedView is not null || _refreshAction != null || _rebuildControlsAction != null) KillTimer(hwnd, RefreshTimerId);
        _refreshAction = null;
        _rebuildControlsAction = null;

        view.Removed();
    }

    private static (int Width, int Height) ToWindowSize(int clientWidth, int clientHeight)
    {
        RECT rect = new() { Left = 0, Top = 0, Right = clientWidth, Bottom = clientHeight };
        AdjustWindowRect(ref rect, WindowStyle, false);
        return (rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    internal static void ResizeWindow(nint hwnd, int clientWidth, int clientHeight)
    {
        (var w, var h) = ToWindowSize(clientWidth, clientHeight);
        SetWindowPos(hwnd, nint.Zero, 0, 0, w, h, SWP_NOMOVE | SWP_NOZORDER);
    }

    private static unsafe nint GetStaticWndProcPointer()
    {
        delegate* unmanaged[Stdcall]<nint, uint, nint, nint, nint> ptr = &WndProc;
        return (nint)ptr;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == WM_TIMER)
        {
            switch (wParam.ToInt32())
            {
                case RefreshTimerId:
                    if (_typedView is not null)
                    {
                        _typedView.RebuildControls();
                    }
                    else
                    {
                        _rebuildControlsAction?.Invoke();
                        if (_rebuildControlsAction is null) _refreshAction?.Invoke();
                    }
                    break;
            }
            return 0;
        }
        if (msg == WM_DESTROY) { PostQuitMessage(0); return 0; }
        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    #region P/Invoke
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEXW
    {
        public uint cbSize; public uint style; public nint lpfnWndProc; public int cbClsExtra; public int cbWndExtra;
        public nint hInstance; public nint hIcon; public nint hCursor; public nint hbrBackground;
        public string? lpszMenuName; public string lpszClassName; public nint hIconSm;
    }
    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MSG { public nint hwnd; public uint message; public nint wParam; public nint lParam; public uint time; public int ptX, ptY; }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern ushort RegisterClassExW(ref WNDCLASSEXW lpwcx);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern nint CreateWindowExW(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, nint hWndParent, nint hMenu, nint hInstance, nint lpParam);
    [DllImport("user32.dll")] private static extern nint LoadCursorW(nint hInstance, int lpCursorName);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern nint GetModuleHandle(string? lpModuleName);
    [DllImport("user32.dll")] private static extern nint DefWindowProcW(nint hWnd, uint msg, nint wParam, nint lParam);
    [DllImport("user32.dll")] private static extern void PostQuitMessage(int nExitCode);
    [DllImport("user32.dll")] private static extern int ShowWindow(nint hWnd, int nCmdShow);
    [DllImport("user32.dll")] private static extern int GetMessageW(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
    [DllImport("user32.dll")] private static extern bool TranslateMessage(ref MSG lpMsg);
    [DllImport("user32.dll")] private static extern nint DispatchMessageW(ref MSG lpMsg);
    [DllImport("user32.dll")] private static extern bool AdjustWindowRect(ref RECT lpRect, uint dwStyle, bool bMenu);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(int value);
    [DllImport("user32.dll")] private static extern nint SetTimer(nint hWnd, int nIDEvent, uint uElapse, nint lpTimerFunc);
    [DllImport("user32.dll")] private static extern bool KillTimer(nint hWnd, int uIDEvent);
    #endregion
}