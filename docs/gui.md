# The GUI library (`TTraxx.PluGui.Gui`)

Everything you need to build a plugin editor: a window base class, a set of audio-oriented controls,
panels, a theme abstraction and the per-platform native windowing underneath. It has no dependency on
any plugin SDK — it draws into a parent handle and communicates through delegates — so the same window
works in a VST 3 host, in the harness, or in anything else that can hand it a native window handle.

- [Mental model](#mental-model)
- [Quick start](#quick-start)
- [The window](#the-window)
- [Controls and configurations](#controls-and-configurations)
- [Parameter binding](#parameter-binding)
- [Panels](#panels)
- [Theming and fonts](#theming-and-fonts)
- [Scaling and design units](#scaling-and-design-units)
- [Writing your own control](#writing-your-own-control)
- [Icons and Skia helpers](#icons-and-skia-helpers)
- [Hosting the window in a VST 3 plugin](#hosting-the-window-in-a-vst-3-plugin)
- [Notes on the API surface](#notes-on-the-api-surface)

Everything lives in the single namespace `TTraxx.PluGui.Gui` (the one exception is
`TTraxx.PluGui.Gui.Helpers.HotReload.GuiHotReload`), so one `using` gets you the whole library.

## Mental model

Four types carry the whole design:

| Type | Role |
| --- | --- |
| `PluginWindow` | The editor. Says *what* it shows (`BuildLayout`) and *what the background looks like* (`DrawBackground`). Owns the native window, the control cache, input routing and painting. |
| `PluginControl` | A knob, toggle, meter, XY pad, or one of your own. Owns runtime state (bounds, hover, drag) and knows how to draw and react to input. |
| `IControlConfiguration` | The *declaration* of a control: what it is bound to, how big it is, whether it is enabled. Long-lived; a control is built from it and cached by its `Id`. |
| `ParameterBinding` | The bridge to your plugin: read the current normalized value, open an edit gesture, write, close. |

Two rules explain most of the API:

1. **Values crossing the boundary are normalized (0..1), never musical units.** Hosts store
   normalized values, and the Hz/dB/ms mapping is your plugin's business. A binding carries
   `MinValue`/`MaxValue` purely so a readout can be formatted.
2. **Lengths you write are unscaled design units, not pixels.** The window rescales them at its own
   content scale, which is why one layout is correct at 100 % and at 200 % DPI.

A configuration's `Id` is its identity: `PluginWindow` caches the control built from it across layout
rebuilds, so **construct each configuration once and reuse the instance**. Building a fresh
configuration on every `BuildLayout()` call throws away every control's state on each rebuild.

## Quick start

```csharp
using SkiaSharp;
using TTraxx.PluGui.Gui;

public sealed class MyPluginWindow(KnobControlConfiguration gainKnob) : PluginWindow
{
    protected override IEnumerable<ControlPlacement> BuildLayout()
    {
        // No width/height: the knob sizes itself from its style's bounds table.
        yield return Place(gainKnob, c => new KnobControl(c), x: 24, y: 24);
    }

    protected override void DrawBackground(SKCanvas canvas, int width, int height)
        => canvas.FillVerticalGradient(0, 0, width, height,
                                       Theme.BackgroundHighlight, Theme.BackgroundShadow);
}
```

…with the configuration built once, wherever your view assembles its content:

```csharp
private double _gain = 0.75;   // normalized; in practice your plugin's parameter object

private readonly KnobControlConfiguration _gainKnob = new()
{
    ControlSize = ControlSizes.M,
    Parameter = new ParameterBinding
    {
        Info = new ParameterControlInfo
        {
            ParameterId = 0,
            Label = "Gain",
            Unit = "dB",
            DefaultNormalizedValue = 0.75
        },
        GetNormalizedValue = () => _gain,
        BeginEdit          = () => host.BeginEdit(0),
        SetNormalizedValue = v => _gain = v,
        EndEdit            = () => host.EndEdit(0),
        MinValue = -60.0,       // display range only
        MaxValue = 6.0
    }
};
```

A host then drives the window through `IPluginWindow`: `AttachToParent(handle, w, h)` once,
`SetBounds` on resize, `RefreshUI()` when parameters change from outside, `Destroy()` on teardown. In
a VST 3 plugin you do not write that yourself — see
[Hosting the window in a VST 3 plugin](#hosting-the-window-in-a-vst-3-plugin).

## The window

Derive from `PluginWindow` and implement two members.

### `BuildLayout()`

Yields every control the window shows, as `ControlPlacement` (control + absolute X/Y + width/height).
It runs once on attach, and again on a hot-reload rebuild — never per frame. Build placements with
`Place` and `PlaceGrid` rather than constructing `ControlPlacement` by hand: they resolve the control
from the cache, resolve its size, and convert panel-relative coordinates to absolute ones.

```csharp
protected ControlPlacement Place<TConfig, TControl>(
    TConfig config, Func<TConfig, TControl> factory,
    int x, int y,
    int? width = null, int? height = null,
    int extraWidth = 0, int extraHeight = 0,
    IPluginPanel? panel = null);
```

Width and height resolve in this order:

1. explicit `width`/`height` arguments;
2. a window-specific override registered with `RegisterSizing<TConfig>(...)` from `ConfigureSizing()`;
3. the configuration's own `ISizedControlConfiguration.ResolveBounds()` — every built-in
   configuration implements this via its style's `Bounds` table, so the common case needs no setup at
   all.

`extraWidth`/`extraHeight` are added on top regardless — handy when a long label needs more room than
the control's natural box. If none of the three applies, `Place` throws with an explanatory message
rather than guessing.

`PlaceGrid` places same-typed controls on a grid; each cell names its own `(Row, Column)`, so a hole
or a ragged row is expressed by simply omitting that cell:

```csharp
(KnobControlConfiguration Config, int Row, int Column)[] cells =
[
    (_attack,  Row: 0, Column: 0),
    (_decay,   Row: 0, Column: 1),
    (_release, Row: 1, Column: 0),   // no (1,1) — that cell stays empty
];

foreach (var placement in PlaceGrid(cells, c => new KnobControl(c),
                                    originX: 16, originY: 16,
                                    rowPitch: 108, columnPitch: 84))
    yield return placement;
```

### `DrawBackground(canvas, width, height)`

Paints everything that is not a control — gradients, dividers, logos, static text — every frame,
before the controls are drawn on top. `width`/`height` are already in pixels. If the window has
panels, call `DrawRegisteredPanels(canvas)` from here so they land beneath their own controls.

### Optional hooks

| Member | Purpose |
| --- | --- |
| `ConfigureSizing()` | Called once before the first `BuildLayout()`; register `RegisterSizing<TConfig>(...)` overrides here. |
| `OnDestroying()` | Called once from `Destroy()`, just before the native window goes away. Release your own resources here. |
| `ParticipatesInDebugBoundsOverlay` | Whether this window honours the harness's control-outline switch. `true` by default. |
| `IsLive` | `true` between a successful attach and `Destroy()`. Check it before touching platform resources from your own timers. |

### Lifecycle

Attaching twice throws (it would leak the first native window — use one window instance per host
view). `Destroy()` is idempotent, and `SetBounds`/`RefreshUI`/`TryFindParameter` quietly no-op once
the window is destroyed, so a host refresh timer racing teardown cannot reach a dead window.

Controls survive rebuilds: they are cached by their configuration's `Id`, and entries a rebuild no
longer references are evicted. `RebuildControls()` is deliberately kept off the everyday surface as an
explicit `IHotReloadTarget` implementation — a production host never calls it.

## Controls and configurations

| Control | Configuration | Interaction |
| --- | --- | --- |
| `KnobControl` | `KnobControlConfiguration` | Vertical drag (Shift = fine-tune), mouse wheel, double-click resets to default, right-click → "Reset to Default". Stepped parameters snap to their positions and draw tick marks. |
| `ToggleControl` | `ToggleControlConfiguration` | Click toggles. Reads *on* at normalized ≥ 0.5 and writes a full 0.0/1.0, so it suits a genuinely two-state parameter. |
| `XYPadControl` | `XYPadControlConfiguration` | One dragged dot drives one parameter per axis (`XParameter`, `YParameter`). Optional `ModulationIndicator` draws a read-only second dot. |
| `MeterControl` | `MeterControlConfiguration` | Display-only. Polls `GetLevel()` while the window repaints continuously; no parameter, nothing to automate. |

Shared on every configuration (from `ControlConfiguration`):

- **`Id`** — generated per instance; the cache key. Construct once, reuse.
- **`ControlSize`** — `ControlSizes.S` (default), `M`, `L` or `XL`. Named buckets rather than free
  pixel sizes, so controls across a plugin stay visually consistent; each style maps them to
  hand-tuned bounds.
- **`IsEnabled`** — a `Func<bool>`, evaluated live. A disabled control is drawn dimmed and ignores
  input, which makes conditional UI (an arp rate knob that only applies while sync is off) a one-liner.

### Styles

Each control has a `Style` property of type `Func<TStyle>?` — `null` means the built-in default. A
factory rather than an instance, so the style is re-resolved on every use: that keeps it live under
Hot Reload and lets it be derived from the current theme.

Styles are records with init-only members, so a variation is a `with` expression:

```csharp
Style = () => KnobStyle.Default with
{
    SweepDeg = 300,                // wider arc
    DragPixelsForFullSweep = 320,  // slower drag
    WheelSteps = 128
}
```

`KnobStyle` covers arc geometry (`StartAngleDeg`, `SweepDeg`), stroke widths, tick radii, the
per-size `Radii` and `Bounds` tables, and the interaction constants (`DragPixelsForFullSweep`,
`FineTuneDivisor`, `WheelSteps`). `MeterStyle` adds segment count/gap, warning and clip thresholds with
their colours, and the peak-hold timing. `XYPadStyle` adds corner radius, glow radii, the indicator
ring and optional `CornerIcons`. `ToggleStyle` currently only carries its `Bounds` table.

**Colours are not in styles** (`MeterStyle`'s explicit level colours aside) — they come from the
window's theme, so one style works against any theme.

## Parameter binding

`ParameterBinding` is the whole contract between a control and your plugin. Build one per parameter
and hand the same instance to every control that should drive it.

| Member | |
| --- | --- |
| `Info` | Required. An `IParameterControlInfo`: id, label, unit, default, step count. |
| `GetNormalizedValue` | Required. Reads the current 0..1 value. |
| `BeginEdit` / `SetNormalizedValue` / `EndEdit` | Required. The edit gesture (see below). |
| `MinValue` / `MaxValue` | Plain-unit range, **display formatting only**. Default 0..1. |
| `ValueFormatter` | Optional `Func<double, string>` overriding the unit-based default formatting. |
| `Normalized` | The current value, clamped to 0..1. |
| `Edit(normalized)` | Convenience for the begin/set/end triplet — for discrete edits (click, wheel, reset). |
| `ToPlain(normalized)` | Linear map onto `MinValue`..`MaxValue`, for readouts. |
| `Quantize(normalized)` | Snaps to the nearest discrete position, or returns the value unchanged when continuous. |

### Why the begin/end brackets matter

A host records a drag as **one** automation gesture. Without `BeginEdit`/`EndEdit` around it, a drag
writes a stream of unrelated value changes and automation recording and undo come out wrong. Controls
already do the right thing (begin on pointer-down, set per move, end on pointer-up); your job is only
to wire the three delegates to the host's equivalents.

For a control that moves two parameters at once, `XYPadControlConfiguration.BeginGroupEdit` /
`EndGroupEdit` wrap the drag in a host-side group so it is recorded as a single user gesture. It is
optional — without it the per-parameter brackets still fire and automation is still correct, just not
grouped. It also matters with NPlug specifically, whose `AudioController` tracks a single
"currently being edited" parameter at a time, so two nested `BeginEdit` calls are not an option.

### Stepped parameters

`ParameterControlInfo.StepCount` is the raw VST 3 step count — the number of *gaps* between min and
max, `0` meaning continuous. For drawing and hit-testing use `PositionCount`, the derived number of
discrete positions (`StepCount + 1`, or `null` when continuous). A knob on a stepped parameter draws
ticks, snaps on drag and moves one position per wheel tick.

### Formatting

By default a value is rendered from `ToPlain()` plus `Info.Unit`, including an automatic Hz → kHz
switch. `ToPlain` is deliberately linear: a parameter with a musical curve applies that curve in the
DSP layer, and a readout derived from the raw range would disagree with it. Supply a `ValueFormatter`
in that case:

```csharp
ValueFormatter = normalized => $"{20.0 * Math.Pow(1000.0, normalized):0} Hz"
```

`ValueFormatter` is also how you label an enum-like parameter ("Up", "Down", "Up-Down", …) rather than
showing a bare index.

## Panels

A panel is background chrome that a group of controls sits on. `PluginPanelConfiguration` describes
the **body** in design units (`Left`, `Top`, `Width`, `Height`, `CornerRadius`, `Title`); a style may
draw its title *outside* that rectangle — `MetallicPluginPanel` puts its header tab in the ~24 design
units *above* `Top` — so leave room above the body.

Construct panels inside `BuildLayout()`, register them for drawing, and pass them to `Place`:

```csharp
protected override IEnumerable<ControlPlacement> BuildLayout()
{
    var arp = RegisterPanel(new MetallicPluginPanel(Context, new PluginPanelConfiguration
    {
        Left = 4, Top = 402, Width = 512, Height = 114, Title = "Arpeggiator"
    }));

    // x/y are panel-relative here; Place still returns an absolute placement.
    yield return Place(_enabledToggle, c => new ToggleControl(c), 16, 4, panel: arp);
    yield return Place(_rateKnob,      c => new KnobControl(c),  156, 4, panel: arp);
}

protected override void DrawBackground(SKCanvas canvas, int width, int height)
{
    // ... background painting ...
    DrawRegisteredPanels(canvas);   // panels draw beneath their controls
}
```

`RegisterPanel` is purely about drawing — a panel's controls still need their own `Place(..., panel:)`
call. Registration is reset on each `BuildLayout()`, so register inside it, not once in a constructor.

For a panel style of your own, derive from `PluginPanel` and implement `Draw`. Build geometry from the
**scaled** accessors (`Left`, `Top`, `Width`, `Height`, `CornerRadius`, and the `FromLeft`/`FromRight`/
`FromTop` helpers), never from the raw configuration, which is unscaled.

## Theming and fonts

`IPluginTheme` is a flat set of `SKColor`s: `Background`, `BackgroundHighlight`/`BackgroundShadow`,
`Accent`/`Accent2`/`AccentDim`, `TextPrimary`/`TextDim`/`TextDisabled`, `TrackBackground`, the glow
triplet, the menu colours and the XY-panel colours. `IMetallicPanelTheme` adds the six metal colours
`MetallicPluginPanel` uses. Implement one type against both interfaces to theme panels too; a theme
that implements only `IPluginTheme` falls back to `DefaultMetallicPanelTheme.Instance`.

Set your theme once at startup, before the first window is built:

```csharp
public static class GuiBootstrapper
{
    static GuiBootstrapper()
    {
        PluginDefaults.Theme = new MyTheme();
        // PluginDefaults.Fonts = new MyFonts();   // optional; DefaultPluginFonts otherwise
    }

    /// Empty body — calling it just forces the static constructor to run.
    public static void EnsureInitialized() { }
}
```

Call `EnsureInitialized()` from your view's `CreateWindow()`, so theme and fonts are in place before
any control is constructed.

`PluginDefaults` holds *defaults*, not the values controls read while drawing: a control always goes
through its window's `RenderContext`, which may carry a theme of its own
(`window.Context.Theme = …`). That indirection is what makes a per-window theme — a host-driven
light/dark switch, say — possible at all.

`IPluginFonts` is just `Regular` and `Bold` `SKTypeface`s. Leave `PluginDefaults.Fonts` alone to use
the built-in ones.

## Scaling and design units

`RenderContext` holds a window's `Scale`, `Theme` and `Fonts`. One instance per window, owned by the
window and handed to every control and panel it lays out. Scale deliberately is **not** global: a DAW
routinely opens two editors of the same plugin on monitors with different DPI, and the plugin binary is
loaded once per process — a static scale factor means the last editor to attach wins, for both.

Convert with `Rescale(designUnits)` for whole pixels and `RescaleExact(designUnits)` for geometry that
should stay sub-pixel exact (thin strokes, gradient stops). Both are available on `PluginWindow`,
`PluginPanel`, `PluginControl` and `RenderContext` itself.

What is in which unit:

- **Design units**: everything in `BuildLayout()` — `Place` coordinates, explicit width/height,
  `extraWidth`/`extraHeight`, grid pitches, everything in a `PluginPanelConfiguration`, and every
  length in a style.
- **Pixels**: `DrawBackground`'s `width`/`height`, a control's `_x`/`_y`/`_w`/`_h` and `Bounds`, a
  panel's scaled accessors, and the coordinates arriving in input handlers.

## Writing your own control

Derive from `PluginControl`, take your own configuration in the primary constructor, and override
`Draw` plus whichever input hooks you react to.

```csharp
public sealed class DotControl(DotControlConfiguration config) : PluginControl(config)
{
    private bool _isOn;

    public override void Draw(SKCanvas canvas)
    {
        using SKPaint paint = new()
        {
            Color = IsEnabled ? (_isOn ? Theme.Accent2 : Theme.AccentDim) : Theme.TextDisabled,
            IsAntialias = true
        };
        // Local coordinates: (0,0) is this control's top-left.
        canvas.DrawCircle(_w / 2f, _h / 2f, RescaleExact(6f), paint);
    }

    public override void OnPointerDown(PointerEventArgs e)
    {
        if (!IsEnabled) return;
        _isOn = !_isOn;
        Refresh();          // request a repaint
    }
}
```

Points to keep in mind:

- **Coordinates.** Bounds (`_x`, `_y`, `_w`, `_h`, `Bounds`) are absolute, window-relative and already
  in pixels; `Draw` and the pointer handlers work in **local** coordinates with (0,0) at the control's
  top-left — the canvas and event positions are translated before they arrive. Run any length of your
  own through `Rescale`/`RescaleExact`.
- **Reuse.** A control is built once and re-bounded any number of times over its life. Do not assume
  a single layout pass.
- **Repaint.** Call `Refresh()` after changing anything visible. Override `NeedsContinuousRepaint` to
  `true` only for something that animates on its own (as `MeterControl` does) — it puts the whole
  window on a repaint timer.
- **Other overrides.** `HitTest(localX, localY)` narrows the clickable area beyond the bounding box
  (the knob restricts it to its disc); `GetParameterInfo` when bound to a parameter — it is what makes
  the host's "what parameter is under this point" query and MIDI-learn menus work;
  `GetContextMenuItems` for a right-click menu (`new ContextMenuItem("Reset to Default", Reset, IsEnabled)`);
  `OnPointerEnter`/`OnPointerLeave` for hover (`IsHovered` is maintained for you); and `Dispose` if you
  cache Skia resources.
- **`SetContainerBackgroundReference`** gives you `_containerW`/`_containerH`, for a control that
  samples the window background to blend against it.

For a matching configuration type, derive from `ParameterControlConfiguration` (parameter-driven) or
`ControlConfiguration` (display-only), and implement `ISizedControlConfiguration.ResolveBounds()` so
`Place` can auto-size it.

## Icons and Skia helpers

`Icons` exposes built-in vector glyphs — `Sine`, `Saw`, `Pulse`, `Triangle`, `ArrowRight`. Every path
is built in a normalized box from −0.5 to +0.5 on both axes, centred on the origin, Y pointing down as
in Skia. Draw them through `SkiaIconExtensions` (`DrawIconFill`, `DrawIconStroke`), which apply the
size and position; passing a path straight to Skia renders a sub-pixel speck at the origin. Paths are
cached and shared process-wide — treat them as read-only and transform a copy, or the canvas.

`SkiaGradientExtensions` covers the fills the built-in controls and panels use:
`FillVerticalGradient` (two colours, or a colour/position array), `FillRoundRectVerticalGradient` and
`FillRoundRectRadialGradient`. `SkiaTextExtensions.DrawTextTopAligned` draws text positioned by its
top edge rather than its baseline, which is what layout code usually wants.

## Hosting the window in a VST 3 plugin

`TTraxx.PluGui.NPlug` (a separate package) implements NPlug's `IAudioPluginView` — which mirrors the
VST 3 `IPlugView` C++ surface — on top of an `IPluginWindow`. Derive from `PluginView<TWindow>`, pass
your editor's base (unscaled) size, and implement one member:

```csharp
public sealed class MyView(MyController controller)
    : PluginView<MyPluginWindow>(baseWidth: 887, baseHeight: 520)
{
    protected override MyPluginWindow CreateWindow()
    {
        GuiBootstrapper.EnsureInitialized();     // theme in place before any control exists
        return new MyPluginWindow(BuildConfigurations());
    }

    protected override void OnEditorClosed() => controller.OnEditorClosed();
}
```

Attach/removed, size and size constraints, content scale, parameter hit-testing and teardown are all
handled for you. Everything else is virtual with a working default:

| Member | Default |
| --- | --- |
| `SupportedPlatforms` | HWND and X11. Narrow it for a single-platform build. |
| `UserUiScale` | `1.0` — an extra multiplier on top of the host's DPI/content scale. |
| `MinScale` / `MaxScale` | `0.5` / `4.0`, clamping what a host may request. |
| `CanResize` | `true`, but `CheckSizeConstraint` forces the host back to the fixed scaled size. |
| `OnEditorClosed` | No-op. Called after the window is destroyed. |
| `OnFocus`, `OnWheel`, `OnKeyDown`, `OnKeyUp` | Ignored. |

`RefreshUI()` repaints to reflect current parameter values — call it from your controller when
parameters change externally (preset load, automation).

### The X11 run loop

X11 has no message loop of its own, so a view there must register an fd-based event handler — and, for
continuously repainting controls such as a meter, a timer — with the host's run loop. `PluginView`
handles the sequencing (both the window and the host frame must exist, and a host may deliver them in
either order) and then calls `OnRegisterRunLoop(frame, eventPump, timerPump)`.

That last step is a hook rather than library code because the VST 3 Linux run-loop interfaces
(`IAudioPluginRunLoop`, `IAudioPluginLinuxEventHandler`, `IAudioPluginLinuxTimerHandler`) exist in the
NPlug sources but not in the published NPlug package this library builds against. If you build against
an NPlug that has them, cast `frame` to `IAudioPluginRunLoop` there, register your handlers and return
`true`; undo it in `OnUnregisterRunLoop`. Everyone else inherits the no-op default and is unaffected —
on Windows and macOS it never fires at all, because those platform windows report no pump sources.

## Notes on the API surface

- **Public API baseline.** `TTraxx.PluGui.Gui` and `TTraxx.PluGui.NPlug` track their public surface in
  `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`, with `RS0016`/`RS0017` as build **errors**. Those
  two files are also the most precise listing of what is public, if you want to skim the whole surface.
- **AOT.** The GUI package is `IsAotCompatible` and reflection-free on the drawing path, so a plugin
  using it can be published NativeAOT. (The harness runners use a little reflection as a fallback —
  they are development-time only and never ship.)
- **Hot Reload.** `GuiHotReload.Reloaded` (in `TTraxx.PluGui.Gui.Helpers.HotReload`) is raised after a
  Hot Reload edit is applied; internal caches for icons and fonts are dropped at the same moment.
  Nothing raises it in a DAW. See the [harness guide](harness.md) for the workflow it belongs to.
- **`Globals.ShowControlBounds`** draws a debug outline around every control. Genuinely process-wide —
  it is a developer's view preference, not a property of a window — and nothing in a shipped plugin
  ever sets it.
