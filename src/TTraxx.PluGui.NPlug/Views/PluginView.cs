using global::NPlug;
using TTraxx.PluGui.Gui;

namespace TTraxx.PluGui.NPlug;

/// <summary>
/// Base class for a plugin's VST3 editor view: implements NPlug's <see cref="IAudioPluginView"/>
/// (which mirrors the VST3 C++ IPlugView surface) on top of an <see cref="IPluginWindow"/>, so a
/// concrete view only has to say WHICH window to show, not how a host attaches, sizes, scales and
/// tears one down.
///
/// A derived view implements one member - <see cref="CreateWindow"/> - and passes its editor's
/// base (unscaled) size to the constructor. Everything else has a working default and is virtual:
/// <list type="bullet">
/// <item><see cref="SupportedPlatforms"/> - HWND and X11 by default; narrow it for a single-platform build.</item>
/// <item><see cref="UserUiScale"/>, <see cref="MinScale"/>, <see cref="MaxScale"/> - the extra UI scale
/// multiplier applied on top of the host's DPI/content scale, and the clamp for it.</item>
/// <item><see cref="OnEditorClosed"/> - called after the window is destroyed, for a controller that
/// wants to know its editor went away.</item>
/// <item><see cref="CanResize"/>, OnFocus/OnWheel/OnKeyDown/OnKeyUp - fixed-size and ignored by default.</item>
/// </list>
///
/// X11 has no message loop of its own, so a view there must register an fd-based event handler AND
/// (for continuously repainting controls) a timer with the host's run loop - but only once both the
/// window and the host frame exist, and the host may deliver those in either order. That sequencing
/// is handled here; the registration itself is delegated to <see cref="OnRegisterRunLoop"/>, because
/// VST3's Linux run-loop interfaces are not part of the NPlug package version this library builds
/// against (see that method's remarks). On Win32/macOS it never fires at all: the platform window
/// reports no pump sources.
/// </summary>
/// <typeparam name="TWindow">The concrete window type this view shows.</typeparam>
public abstract class PluginView<TWindow> : IPluGuiPluginView
    where TWindow : class, IPluginWindow
{
    private readonly int _baseWidth;
    private readonly int _baseHeight;

    private TWindow? _window;
    private IAudioPluginFrame? _frame;

    private bool _isAttached;
    private bool _runLoopRegistered;

    /// <param name="baseWidth">The editor's design width, in unscaled units (multiplied by <see cref="Scale"/> for the host).</param>
    /// <param name="baseHeight">The editor's design height, in unscaled units.</param>
    protected PluginView(int baseWidth, int baseHeight)
    {
        _baseWidth = baseWidth;
        _baseHeight = baseHeight;
    }

    /// <summary>The window this view drives. Null until the host has attached the view.</summary>
    protected TWindow? Window => _window;

    /// <summary>
    /// The content scale this editor is currently drawn at: the host's DPI/content scale times
    /// <see cref="UserUiScale"/>. Owned by the view (the host asks for our size before there's a
    /// window to ask), and mirrored into the window's <see cref="RenderContext"/> as it changes.
    /// </summary>
    public float Scale { get; private set; } = 1.0f;

    // Single place where scale changes, so the window's context can never drift from the view's.
    private void SetScale(float scale)
    {
        Scale = scale;
        if (_window is not null) _window.Context.Scale = scale;
    }

    /// <summary>The host frame, once it has handed us one. Null before <see cref="SetFrame"/>.</summary>
    protected IAudioPluginFrame? Frame => _frame;

    /// <summary>Platform handle types this view can be embedded in. HWND and X11 by default.</summary>
    protected virtual AudioPluginViewPlatform[] SupportedPlatforms { get; } =
    [
        AudioPluginViewPlatform.Hwnd,
        AudioPluginViewPlatform.X11EmbedWindowID
    ];

    /// <summary>Extra user multiplier applied on top of the host's DPI/content scale. 1.0 = follow the host exactly.</summary>
    protected virtual float UserUiScale => 1.0f;

    /// <summary>Lower clamp for the scale a host may request via <see cref="SetContentScaleFactor"/>.</summary>
    protected virtual float MinScale => 0.5f;

    /// <summary>Upper clamp for the scale a host may request via <see cref="SetContentScaleFactor"/>.</summary>
    protected virtual float MaxScale => 4.0f;

    /// <summary>Creates the window this view shows. Called once, when the host attaches the view.</summary>
    protected abstract TWindow CreateWindow();

    /// <summary>
    /// Called at the end of <see cref="Removed"/>, after the window is destroyed - e.g. for a
    /// controller that tracks whether its editor is open. No-op by default.
    /// </summary>
    protected virtual void OnEditorClosed() { }

    public virtual ViewRectangle Size
    {
        get
        {
            var (w, h) = GetScaledSize();
            return new ViewRectangle(0, 0, w, h);
        }
    }

    public virtual bool IsPlatformTypeSupported(AudioPluginViewPlatform type)
        => Array.IndexOf(SupportedPlatforms, type) >= 0;

    public virtual void Attached(nint parent, AudioPluginViewPlatform type)
    {
        if (!IsPlatformTypeSupported(type)) return;

        _window ??= CreateWindow();

        // A host may have called SetContentScaleFactor before attaching, so the fresh window's
        // context starts from whatever scale we already know rather than from 1.0.
        _window.Context.Scale = Scale;

        _window.AttachToParent(parent, _baseWidth, _baseHeight);
        _isAttached = true;

        TryRegisterRunLoop();

        // Determine initial scale (host DPI/content scale) and apply user multiplier.
        SetScale(_window.GetInitialScaleFactor(parent) * UserUiScale);

        // Ask host to resize and layout the child window
        ApplyScaleAndResize();

        // Refresh UI to ensure controls reflect current parameter values
        // (important when the editor is reopened after host preset changes)
        _window.RefreshUI();
    }

    public virtual void SetFrame(IAudioPluginFrame frame)
    {
        _frame = frame;

        TryRegisterRunLoop();

        // Ensure host gets our preferred size if the frame arrives after Attached
        ApplyScaleAndResize();
    }

    public virtual void Removed()
    {
        if (_runLoopRegistered)
        {
            OnUnregisterRunLoop();
            _runLoopRegistered = false;
        }
        _isAttached = false;

        _window?.Destroy();

        OnEditorClosed();
    }

    public virtual void OnSize(ViewRectangle newSize)
    {
        // Host-driven size change: fit the child
        var w = Math.Max(1, newSize.Right - newSize.Left);
        var h = Math.Max(1, newSize.Bottom - newSize.Top);
        _window?.SetBounds(0, 0, w, h);
        _window?.RefreshUI();
    }

    public virtual bool CanResize() => true;

    public virtual bool CheckSizeConstraint(ref ViewRectangle rect)
    {
        // Force the host to our fixed, scaled size
        var (w, h) = GetScaledSize();
        rect = new ViewRectangle(rect.Left, rect.Top, rect.Left + w, rect.Top + h);
        return true;
    }

    public virtual void OnFocus(bool state) { }

    public virtual void OnWheel(float distance) { }

    public virtual void OnKeyDown(ushort key, short keyCode, short modifiers) { }

    public virtual void OnKeyUp(ushort key, short keyCode, short modifiers) { }

    public virtual void SetContentScaleFactor(float factor)
    {
        // Host-provided content scale (e.g. DPI). Apply user multiplier and clamp.
        SetScale(Math.Clamp(factor * UserUiScale, MinScale, MaxScale));
        ApplyScaleAndResize();
    }

    /// <summary>
    /// Lets the host (e.g. for right-click "assign to quick control" / MIDI-learn menus) ask
    /// "what parameter lives at this point?".
    /// </summary>
    public virtual bool TryFindParameter(int xPos, int yPos, out AudioParameterId parameterId)
    {
        parameterId = default;
        if (_window is null) return false;

        var result = _window.TryFindParameter(xPos, yPos, out var paramId);
        parameterId = paramId;
        return result;
    }

    /// <summary>
    /// Repaints to reflect current parameter values. Called when parameters change externally
    /// (host preset loading, automation).
    /// </summary>
    public void RefreshUI() => _window?.RefreshUI();

    /// <summary>Hot-reload only (see IHotReloadTarget); a no-op for a window that doesn't support it.</summary>
    public void RebuildControls() => (_window as IHotReloadTarget)?.RebuildControls();

    /// <summary>
    /// Only relevant for non-host test harnesses (like the GUI harness): without a real VST3 run
    /// loop, <see cref="TryRegisterRunLoop"/> never fires, so such a harness must pump events itself.
    /// Null on platforms that have their own message loop (Win32, macOS).
    /// </summary>
    public IEventPumpSource? EventPumpSource => _window?.EventPumpSource;

    /// <summary>
    /// Some platforms (X11/Linux) have no native message loop and must be polled via the host run
    /// loop on an fd (see <see cref="IEventPumpSource"/>). The same platform also has no timer of
    /// its own to drive a continuously repainting control, e.g. a level meter (see
    /// <see cref="ITimerPumpSource"/>). Other platforms (Win32, macOS) leave both null, making this
    /// a no-op for them - no platform check needed, the abstraction handles it.
    ///
    /// Called from both <see cref="Attached"/> and <see cref="SetFrame"/> because the host may call
    /// them in either order, and registration needs both the window and the frame.
    /// </summary>
    protected void TryRegisterRunLoop()
    {
        if (_runLoopRegistered) return;              // already registered
        if (!_isAttached || _window is null) return; // window not ready yet
        if (_frame is null) return;                  // host hasn't given us a frame yet

        // Nothing to pump on this platform - don't bother the derived view (or mark ourselves
        // registered, so a later platform that does report a source still gets its chance).
        var eventPump = _window.EventPumpSource;
        var timerPump = _window.TimerPumpSource;
        if (eventPump is null && timerPump is null) return;

        // Only latch if the hook actually registered something: a host that hands us a frame which
        // isn't a run loop may still deliver a usable one on a later SetFrame().
        _runLoopRegistered = OnRegisterRunLoop(_frame, eventPump, timerPump);
    }

    /// <summary>
    /// Registers <paramref name="eventPump"/> / <paramref name="timerPump"/> with the host's run
    /// loop. Called at most once per attach, as soon as both the window and
    /// <paramref name="frame"/> exist and at least one pump source is non-null - i.e. on X11 only.
    /// No-op by default.
    ///
    /// REMARKS: this is a hook rather than library code because the VST3 Linux run-loop interfaces
    /// (IAudioPluginRunLoop, IAudioPluginLinuxEventHandler, IAudioPluginLinuxTimerHandler) exist in
    /// the NPlug sources but not in the published NPlug package this library builds against. A
    /// plugin building against an NPlug that has them casts <paramref name="frame"/> to
    /// IAudioPluginRunLoop here and registers its own handlers; everyone else inherits the no-op and
    /// is unaffected. Fold this back into the base class once those types ship in a released NPlug.
    /// </summary>
    /// <returns>
    /// <c>true</c> if anything was registered. Returning <c>false</c> (the default) leaves the view
    /// unlatched, so a later <see cref="SetFrame"/> gets another chance - e.g. when this frame turned
    /// out not to be a run loop.
    /// </returns>
    protected virtual bool OnRegisterRunLoop(IAudioPluginFrame frame, IEventPumpSource? eventPump, ITimerPumpSource? timerPump) => false;

    /// <summary>
    /// Undoes <see cref="OnRegisterRunLoop"/>. Called from <see cref="Removed"/>, and only if a
    /// registration actually happened. No-op by default.
    /// </summary>
    protected virtual void OnUnregisterRunLoop() { }

    /// <summary>The editor's base size with the current <see cref="Scale"/> applied.</summary>
    protected (int Width, int Height) GetScaledSize()
        => ((int)Math.Round(Scale * _baseWidth), (int)Math.Round(Scale * _baseHeight));

    /// <summary>
    /// Asks the host to resize the editor to our scaled size, then matches the child window to it.
    /// Call after anything that changes <see cref="Scale"/>.
    /// </summary>
    protected void ApplyScaleAndResize()
    {
        var (w, h) = GetScaledSize();
        ViewRectangle rect = new(0, 0, w, h);

        // Ask host to resize the editor if possible
        _frame?.ResizeView(this, rect);

        // Ensure our child window matches whatever the host will use
        _window?.SetBounds(0, 0, w, h);
        _window?.RefreshUI();
    }
}
