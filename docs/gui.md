# The GUI library (`TTraxx.PluGui.Gui`)

Everything you need to build a plugin editor: a window base class, a set of audio-oriented controls,
panels, a theme abstraction and the per-platform native windowing underneath. It has no dependency on
any plugin SDK — it draws into a parent handle and communicates through delegates — so the same window
works in a VST 3 host, in the harness, or in anything else that can hand it a native window handle.

- [Mental model](#mental-model)
- [Quick start (NPlug)](#quick-start-nplug)
- [The window](#the-window)
- [Controls and configurations](#controls-and-configurations)
- [Parameter binding](#parameter-binding)
- [Panels](#panels)
- [Theming and fonts](#theming-and-fonts)
- [Scaling and design units](#scaling-and-design-units)
- [Writing your own control](#writing-your-own-control)
- [Icons and Skia helpers](#icons-and-skia-helpers)
- [Hosting the window in a VST 3 plugin](#hosting-the-window-in-a-vst-3-plugin)
- [Hosting the window without NPlug](#hosting-the-window-without-nplug)
- [Shipping the plugin](#shipping-the-plugin)
- [Platform notes](#platform-notes)
- [Notes on the API surface](#notes-on-the-api-surface)

Almost everything lives in the single namespace `TTraxx.PluGui.Gui`, so one `using` gets you nearly the
whole library. The two exceptions are `TTraxx.PluGui.Gui.Helpers.HotReload.GuiHotReload` and
`TTraxx.PluGui.Gui.Controls.Factories.ControlFactory`. The VST 3 bridge (`PluginView<TWindow>`) is in
`TTraxx.PluGui.NPlug`, in its own package.

## Mental model

Four types carry the whole design:

| Type | Role |
| --- | --- |
| `PluginWindow` | The editor. Says *what* it shows (`BuildLayout`) and *what the background looks like* (`DrawBackground`). Owns the native window, the control cache, input routing and painting. |
| `PluginControl` | A knob, slider, toggle, meter, XY pad, or one of your own. Owns runtime state (bounds, hover, drag) and knows how to draw and react to input. |
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

## Quick start (NPlug)

The library is SDK-agnostic, but the only bridge it ships is for [NPlug](https://github.com/xoofx/NPlug),
so this walks through the NPlug case end to end: one gain knob in an editor. It assumes you already
have an NPlug model and controller; only the parts PluGui touches are shown.

**1. The window** — says what to show and paints the background:

```csharp
using SkiaSharp;
using TTraxx.PluGui.Gui;

public sealed class MyPluginWindow(KnobControlConfiguration gainKnob) : PluginWindow
{
    protected override IEnumerable<ControlPlacement> BuildLayout()
    {
        // No width/height: the knob sizes itself from its style's size table.
        yield return Place(gainKnob, c => new KnobControl(c), x: 24, y: 24);
    }

    protected override void DrawBackground(SKCanvas canvas, int width, int height)
        => canvas.FillVerticalGradient(0, 0, width, height,
                                       Theme.BackgroundHighlight, Theme.BackgroundShadow);
}
```

**2. The view** — the VST 3 editor. It builds the configurations (once per window) and binds each
control to an NPlug parameter:

```csharp
using NPlug;
using TTraxx.PluGui.Gui;
using TTraxx.PluGui.NPlug;

public sealed class MyView(MyController controller, MyModel model)
    : PluginView<MyPluginWindow>(baseWidth: 200, baseHeight: 150)   // editor size in design units
{
    protected override MyPluginWindow CreateWindow() => new(new KnobControlConfiguration
    {
        ControlSize = ControlSizes.M,
        Parameter = Bind(model.Gain, min: -60.0, max: 6.0)   // min/max: readout only
    });

    protected override void OnEditorClosed() => controller.OnEditorClosed();

    // One binding per NPlug parameter. Worth keeping as a helper: every control needs one.
    private ParameterBinding Bind(AudioParameter p, double min = 0.0, double max = 1.0) => new()
    {
        Info = new ParameterControlInfo
        {
            ParameterId = p.Id.Value,
            Label = p.Title,
            Unit = p.Units,
            DefaultNormalizedValue = p.DefaultNormalizedValue,
            StepCount = p.StepCount
        },
        GetNormalizedValue = () => p.NormalizedValue,
        BeginEdit          = () => controller.BeginEditParameter(p),
        SetNormalizedValue = v => p.NormalizedValue = v,
        EndEdit            = () => controller.EndEditParameter(),
        MinValue = min,
        MaxValue = max
    };
}
```

**3. The controller** — creates the view and keeps it in step with changes that don't come from the
editor (automation, preset load):

```csharp
public sealed class MyController : AudioController<MyModel>
{
    private MyView? _view;

    protected override IAudioPluginView? CreateView() => _view = new MyView(this, Model);

    protected override void OnParameterValueChanged(AudioParameter parameter, bool parameterValueChangedFromHost)
    {
        base.OnParameterValueChanged(parameter, parameterValueChangedFromHost);
        _view?.RefreshUI();
    }

    internal void OnEditorClosed() => _view = null;
}
```

Call `_view?.RefreshUI()` from your state-restore overrides (`RestoreState`,
`RestoreComponentState`) as well, so a preset load repaints the editor.

That is a working editor on Windows. Linux additionally needs the
[X11 run-loop hook](#the-x11-run-loop); macOS is untested and not enabled by default — see
[Platform notes](#platform-notes). Before you ship, read [Shipping the plugin](#shipping-the-plugin):
the native Skia library has to travel with your plugin. To iterate on the layout without a DAW, use
the [GUI harness](harness.md).

## The window

Derive from `PluginWindow` and implement two members.

### `BuildLayout()`

Yields every control the window shows, as `ControlPlacement` (control + X/Y + width/height, all in
design units). It runs once on attach, and again on a hot-reload rebuild — never per frame. Build
placements with `Place` and `PlaceGrid` rather than constructing `ControlPlacement` by hand: they
resolve the control from the cache, resolve its size, and convert panel-relative coordinates to
window-relative ones.

```csharp
protected ControlPlacement Place<TConfig, TControl>(
    TConfig config, Func<TConfig, TControl> factory,
    int x, int y,
    int? width = null, int? height = null,
    int extraWidth = 0, int extraHeight = 0,
    IPluginPanel? panel = null,
    Func<bool>? visibleWhen = null);
```

`factory` builds the control the first time this configuration is placed; later builds reuse the
cached control and do not call it again. For the built-in controls it is always
`c => new XxxControl(c)`.

Width and height resolve in this order:

1. explicit `width`/`height` arguments (give just one and the other still resolves from the steps
   below);
2. a window-specific override registered with `RegisterSizing<TConfig>(...)` from `ConfigureSizing()`;
3. the configuration's own `ISizedControlConfiguration.ResolveBounds()` — every built-in
   configuration gets this from `IStyledControlConfiguration<TStyle>`, which reads the style's
   `Sizes` table, so the common case needs no setup at all.

`extraWidth`/`extraHeight` are added on top regardless — handy when a long label needs more room than
the control's natural box. If none of the three applies, `Place` throws with an explanatory message
rather than guessing.

`visibleWhen` shows the control only while it returns `true`. It is evaluated on every draw and
hit-test, and a hidden control is neither drawn nor clickable — the building block for
[paged panels](#tab-strips-and-paged-panels). `null` (the default) means always visible.

`PlaceGrid` places same-typed controls on a grid; each cell names its own `(Row, Column)`, so a hole
or a ragged row is expressed by simply omitting that cell. Its remaining parameters behave exactly as
on `Place`:

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
before the controls are drawn on top. `width`/`height` are already in pixels, and so is the canvas:
run your own design-unit lengths through `Rescale`/`RescaleExact`. If the window has panels, call
`DrawRegisteredPanels(canvas)` from here so they land beneath their own controls.

### Optional hooks and helpers

| Member | Purpose |
| --- | --- |
| `ConfigureSizing()` | Called once before the first `BuildLayout()`; register `RegisterSizing<TConfig>(...)` overrides here. |
| `OnDestroying()` | Called once from `Destroy()`, just before the native window goes away. Release your own resources here. |
| `ParticipatesInDebugBoundsOverlay` | Whether this window honours the harness's control-outline switch. `true` by default. |
| `IsLive` | `true` between a successful attach and `Destroy()`. Check it before touching platform resources from your own timers. |
| `Context`, `Theme`, `MetallicTheme`, `Fonts` | This window's `RenderContext` and the theme and fonts it resolves to. |
| `Rescale`, `RescaleExact` | Design units to pixels at this window's scale — see [Scaling](#scaling-and-design-units). |

### Lifecycle

Attaching twice throws (it would leak the first native window — use one window instance per host
view). `Destroy()` is idempotent, and `SetBounds`/`RefreshUI`/`TryFindParameter` quietly no-op once
the window is destroyed, so a host refresh timer racing teardown cannot reach a dead window.

Controls survive rebuilds: they are cached by their configuration's `Id`, and entries a rebuild no
longer references are evicted and disposed. `RebuildControls()` is deliberately kept off the everyday
surface as an explicit `IHotReloadTarget` implementation — a production host never calls it.

## Controls and configurations

| Control | Configuration | Bound to | Interaction |
| --- | --- | --- | --- |
| `KnobControl` | `KnobControlConfiguration` | `Parameter` | Vertical drag (Shift = fine-tune), mouse wheel, double-click resets to default. Stepped parameters snap to their positions and draw tick marks. |
| `SliderControl` | `SliderControlConfiguration` | `Parameter` | Horizontal bar with the value printed on it. Relative horizontal drag (Shift = fine-tune), wheel, double-click resets. `IsBipolar` fills from the centre. |
| `ToggleControl` | `ToggleControlConfiguration` | `Parameter` | Click toggles. Reads *on* at normalized ≥ 0.5 and writes a full 0.0/1.0, so it suits a genuinely two-state parameter. |
| `DropdownControl` | `DropdownControlConfiguration` | `Parameter` (stepped) + `Items` | Click opens the list with the current choice checked; the wheel steps through the choices without opening it. |
| `XYPadControl` | `XYPadControlConfiguration` | `XParameter` + `YParameter` | Drag places the dot under the pointer, driving one parameter per axis. See [XY pad](#xy-pad). |
| `TabStripControl` | `TabStripControlConfiguration` | Delegates, no parameter | Click selects a tab. See [Tab strips and paged panels](#tab-strips-and-paged-panels). |
| `ButtonControl` | `ButtonControlConfiguration` | `OnClick`, no parameter | Runs `OnClick` on release over the button, so a press can be abandoned by dragging off. For one-shot commands, which change the plugin through its own parameters. `OnPress`/`OnRelease` make it momentary instead: they run on pointer down and on the matching pointer up (wherever it lands), for an action that lasts as long as the button is held — auditioning something, say. |
| `MeterControl` | `MeterControlConfiguration` | `GetLevel`, no parameter | Display-only. Polls `GetLevel()` while the window repaints continuously; nothing to automate. |

Every parameter-bound control (knob, slider, toggle, dropdown, XY pad) also offers a right-click
**Reset to Default** menu, and reports its parameter to the host's "what is under the mouse" query.
Button, tab strip and meter do not.

Shared on every configuration (from `ControlConfiguration`):

- **`Id`** — generated per instance; the cache key. Construct once, reuse.
- **`ControlSize`** — `ControlSizes.S` (default), `M`, `L` or `XL`. Named buckets rather than free
  pixel sizes, so controls across a plugin stay visually consistent; each style maps them to
  hand-tuned bounds.
- **`IsEnabled`** — a `Func<bool>`, evaluated live. A disabled control is drawn dimmed and ignores
  input, which makes conditional UI (an arp rate knob that only applies while sync is off) a one-liner.
- **`Theme`** — a `Func<IPluginTheme>?` for this control alone; `null` (the default) uses its
  window's. For a group of controls that should read as a different kind of thing from the rest — a
  tool rather than a sound control, say — in a different accent. See [Theming and fonts](#theming-and-fonts).

Knob, toggle and meter boxes include room for their label, and share their heights per tier, so they
line up in a row. Slider, dropdown, tab strip and button are compact bars (22 or 17 units high) whose
default width is only a starting point — you normally pass an explicit `width` to `Place`.

### Dropdown

For a stepped parameter whose positions have names (a waveform, a modulation source). The parameter's
`StepCount` decides how many positions there are; `Items` names them in order. `Items` may be *shorter*
than the number of positions: the extra positions are reserved, never offered in the list, and shown
as "-" if the host sets one. That lets a list grow later without changing what stored values mean.

```csharp
new DropdownControlConfiguration
{
    Parameter = Bind(model.Waveform),          // StepCount = 3 → four positions
    Items = ["Sine", "Saw", "Pulse", "Triangle"]
}
```

Double-click does not reset a dropdown (the first click of a double-click already opened the list);
use the right-click menu.

### XY pad

One dot, two parameters: `XParameter` follows the horizontal position (0 = left edge) and
`YParameter` the vertical position (**0 = top edge**, 1 = bottom). A drag places the dot absolutely
under the pointer. Everything else is optional:

| Member | Effect |
| --- | --- |
| `BeginGroupEdit` / `EndGroupEdit` | Wrap a drag in a host-side edit group — see [below](#why-the-beginend-brackets-matter). |
| `Snap` | `(x, y) => (x, y)` quantizer applied to dragged positions (and to Reset to Default) before they are written — to snap to corners or a grid, say. Read on every use, so it can follow a mode parameter. |
| `SnapPoints` | Where `Snap` can land, for display: a point inside the pad draws as a dot, one on an edge as a tick, one on a corner lights that corner's icon. |
| `ModulationIndicator` | A read-only second dot plus connecting line, offset from the main dot by two bipolar (−1..1) delegates. |
| `Footer` | A segmented selector (`XYPadFooter`: labels + get/set index delegates) drawn inside the pad's frame, below the pad area; adds `XYPadStyle.FooterHeight` to the control's height. Its optional `Toggle` (`XYPadFooterToggle`) adds an on/off switch at the right end. Neither is bound to a parameter — the delegates decide what they do. |
| `LivePoints` | Read-only points (`XYPadLivePoint`: id, x, y, weight) showing where values actually are — per-voice modulation, say — each with a short fading trail, in the theme's `XYLivePoint` colour. While set, the pad repaints continuously. Give a point a new `Id` when it stands for something new (a new note), or its trail will connect to the old one. |

The pad has no wheel or double-click behaviour. `XYPadStyle.CornerIcons` draws the waveform glyphs in
the corners by default (order: top-left, top-right, bottom-left, bottom-right); set it to `null` for a
pad whose corners mean nothing.

### Tab strips and paged panels

A tab strip is not bound to a parameter: which page is showing is window state, not part of the
sound. You own that state and hand the strip two delegates; each control on a page is placed with a
`visibleWhen` that checks it. Because the delegates capture the window's own field, build the
configuration in the window's constructor:

```csharp
public sealed class MyPluginWindow : PluginWindow
{
    private readonly MyConfigurations _c;
    private readonly TabStripControlConfiguration _modTabs;
    private int _modPage;   // window state: not a parameter, not saved with a preset

    public MyPluginWindow(MyConfigurations configurations)
    {
        _c = configurations;
        _modTabs = new TabStripControlConfiguration
        {
            Tabs = ["Matrix", "LFO"],
            GetSelectedIndex = () => _modPage,
            SetSelectedIndex = page => _modPage = page   // the strip repaints the window itself
        };
    }

    protected override IEnumerable<ControlPlacement> BuildLayout()
    {
        var mod = RegisterPanel(new MetallicPluginPanel(Context, new PluginPanelConfiguration
        {
            Left = 4, Top = 30, Width = 400, Height = 120, Title = "Modulation"
        }));

        // A negative panel-relative y reaches up into the panel's 24-unit title band.
        yield return Place(_modTabs, c => new TabStripControl(c), 240, -21, width: 150, panel: mod);

        yield return Place(_c.MatrixAmount, c => new SliderControl(c), 16, 16, width: 160, panel: mod,
                           visibleWhen: () => _modPage == 0);
        yield return Place(_c.LfoRate, c => new KnobControl(c), 16, 8, panel: mod,
                           visibleWhen: () => _modPage == 1);
    }

    // DrawBackground as usual, calling DrawRegisteredPanels(canvas).
}
```

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

All lengths in a style are design units.

#### Size tables

Everything that changes with `ControlSize` lives in one `SizeTable<T>` per style, `Sizes` — one
entry per tier. Every tier is `required`, so a table can't leave a size out and no lookup can miss
at runtime. A control whose look doesn't follow the tier uses `SizeTable<T>.Uniform(value)`.

What an entry holds depends on the control: most styles just store the layout box,
`SizeTable<(int Width, int Height)>`; `KnobStyle` stores a `KnobMetrics` (layout box, knob diameter,
track and value stroke widths) and `ToggleStyle` a `ToggleMetrics` (layout box, pill height and the
pill's vertical centre), so a tier's box and its drawing geometry can't come from different tables.
The rule of thumb when adding a style value: if it varies with the size tier it goes in the entry
type, otherwise it is a plain property on the style.

Overriding one tier is a nested `with`:

```csharp
Style = () => KnobStyle.Default with
{
    //                                          Width Height Diameter Track Value
    Sizes = KnobStyle.Default.Sizes with { XL = new(80,   124,   70,      3.0f, 4.6f) }
}
```

#### Built-in styles

| Style | What it covers besides `Sizes` |
| --- | --- |
| `KnobStyle` | Arc geometry (`StartAngleDeg`, `SweepDeg`), step-tick radii, and the interaction constants (`DragPixelsForFullSweep`, `FineTuneDivisor`, `WheelSteps`). |
| `SliderStyle` | `CornerRadius`, `FontSize`, and the same three interaction constants as the knob. |
| `ToggleStyle` | `Appearance`: `Pill` (default) or `Chip` — a labelled box that fills its placement and lights up while on, for rows of related switches; give it an explicit width and height. A chip also takes `ChipCornerRadius`, `ChipFontSize` and `ChipOffIcon` (a glyph before the label while off — `Icons.Padlock` for "locked", say). |
| `DropdownStyle` | `CornerRadius`, `Padding`, `ChevronWidth`, `FontSize`. |
| `TabStripStyle` | `CornerRadius`, `FontSize`. |
| `ButtonStyle` | `CornerRadius`, `FontSize`, an optional `Icon` before the label (sized by `IconScale`, a multiple of `FontSize`), and `IsPrimary` — tinted and outlined with the accent at rest, for an area's main action. |
| `MeterStyle` | Segment count and gap, warning/clip thresholds with their colours, a silence floor, and the peak-hold timing. |
| `XYPadStyle` | Corner radius, glow and indicator radii, `CornerIcons` and their size, the footer's height/font/toggle width, live-point radius and trail length, snap-marker sizes. |

**Colours are not in styles** (`MeterStyle`'s fixed green/amber/red aside) — they come from the
window's theme, so one style works against any theme.

## Parameter binding

`ParameterBinding` is the whole contract between a control and your plugin. Build one per parameter
and hand the same instance to every control that should drive it. The
[quick start](#quick-start-nplug) shows a complete binding for an NPlug `AudioParameter`.

| Member | |
| --- | --- |
| `Info` | Required. An `IParameterControlInfo` — normally a `ParameterControlInfo` with id, label, unit, default and step count. |
| `GetNormalizedValue` | Required. Reads the current 0..1 value. |
| `BeginEdit` / `SetNormalizedValue` / `EndEdit` | Required. The edit gesture (see below). |
| `MinValue` / `MaxValue` | Plain-unit range, **display formatting only**. Default 0..1. |
| `ValueFormatter` | Optional `Func<double, string>` replacing the unit-based default formatting. Receives the *plain* value — see [Formatting](#formatting). |
| `Normalized` | The current value, clamped to 0..1. |
| `Edit(normalized)` | Convenience for the begin/set/end triplet — for discrete edits (click, wheel, reset). |
| `ToPlain(normalized)` | Linear map onto `MinValue`..`MaxValue`, for readouts. |
| `Quantize(normalized)` | Snaps to the nearest discrete position, or returns the value unchanged when continuous. |

### Why the begin/end brackets matter

A host records a drag as **one** automation gesture. Without `BeginEdit`/`EndEdit` around it, a drag
writes a stream of unrelated value changes and automation recording and undo come out wrong. The
built-in controls already do the right thing; your job is only to wire the three delegates to the
host's equivalents (with NPlug: `BeginEditParameter`, setting `NormalizedValue`, `EndEditParameter`).

Knobs and sliders open one bracket for the whole drag (begin on pointer-down, set per move, end on
pointer-up). The XY pad cannot: it moves two parameters, and NPlug's `AudioController` tracks only one
"currently being edited" parameter at a time, so nested `BeginEdit` calls are not an option. It
therefore writes each move as a complete begin/set/end per parameter. To have the host record the
whole drag as one gesture anyway, set `BeginGroupEdit`/`EndGroupEdit` on the pad (with NPlug:
`BeginGroupEditParameters`/`EndGroupEditParameters`). It is optional — without it automation is still
correct, just not grouped.

### Stepped parameters

`ParameterControlInfo.StepCount` is the raw VST 3 step count — the number of *gaps* between min and
max, `0` meaning continuous. For drawing and hit-testing use `PositionCount`, the derived number of
discrete positions (`StepCount + 1`, or `null` when continuous). A knob on a stepped parameter draws
ticks, snaps on drag, moves one position per wheel tick and shows the current value as its readout.

### Formatting

By default a value is rendered from `ToPlain()` plus `Info.Unit`, with the number of decimals chosen
by unit (`"Hz"`, `"kHz"`, `"dB"`, `"ms"`, `"s"`, `"%"`, `"oct"`) and an automatic Hz → kHz switch
from 1000 Hz up.

`ValueFormatter` replaces that. **It receives the plain value — `ToPlain(normalized)` — not the
normalized one.** With the default 0..1 range the two are the same number; once you set
`MinValue`/`MaxValue`, the formatter sees values in that range.

`ToPlain` is deliberately linear: a parameter with a musical curve applies that curve in the DSP
layer, and a readout derived from the raw range would disagree with it. For such a parameter, leave
the range at 0..1 (or convert back) and apply the curve in the formatter:

```csharp
// Frequency with an exponential 20 Hz..20 kHz curve in the DSP. MinValue/MaxValue left at 0..1,
// so the formatter receives the normalized position.
ValueFormatter = position => $"{20.0 * Math.Pow(1000.0, position):0} Hz"
```

A knob draws its range ends (formatter applied to 0.0 and 1.0 of the range) as well as the live value,
so the formatter should produce short strings.

`ValueFormatter` is also how you label an enum-like parameter shown on a knob rather than showing a
bare index. Set the range to the index range, so the plain value *is* the index:

```csharp
MinValue = 0,
MaxValue = patterns.Length - 1,
ValueFormatter = index => patterns[Math.Clamp((int)Math.Round(index), 0, patterns.Length - 1)]
```

(A `DropdownControl` takes its names from `Items` instead and ignores the formatter.)

## Panels

A panel is background chrome that a group of controls sits on: a titled card. It draws behind its
controls and gives them a coordinate origin, but it does not own, clip or dispose them.

Two styles are built in:

- **`MetallicPluginPanel`** — a brushed-metal body under the title band. The primary look.
- **`TransparentPluginPanel`** — the same title band and outline with no body fill, so the window
  background shows through. For a group that should read as subordinate to the metal panels around it.

`PluginPanelConfiguration` describes the **body** in design units (`Left`, `Top`, `Width`, `Height`,
`CornerRadius`, and a required `Title` — pass `""` for an untitled frame). Every panel draws its title
band in the **24 design units above `Top`**, so leave room above the body.

Construct panels inside `BuildLayout()`, register them for drawing, and pass them to `Place`:

```csharp
protected override IEnumerable<ControlPlacement> BuildLayout()
{
    var arp = RegisterPanel(new MetallicPluginPanel(Context, new PluginPanelConfiguration
    {
        Left = 4, Top = 402, Width = 512, Height = 114, Title = "Arpeggiator"
    }));

    // x/y are relative to the panel body's top-left here; Place converts them.
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
call. Registration is reset on each `BuildLayout()`, so construct and register panels inside it, never
once in a constructor or a field: a panel kept across builds keeps accumulating its children.

To label rows inside a panel, call `panel.DrawSectionDivider(canvas, relativeY, "LABEL")` from
`DrawBackground`, *after* `DrawRegisteredPanels`. It draws a centred "──── LABEL ────" line at
`relativeY` design units below the body's top. Keep the panel in a field for that, assigned in
`BuildLayout()` so it is the current build's instance.

For a panel style of your own, derive from `PluginPanel` and override `DrawBody(canvas)`. The base
class draws the title band, the outline and the title, and clips the canvas to the body's rounded
outline before calling you, so fill a plain rectangle. Build geometry from the **scaled** accessors
(`Left`, `Top`, `Width`, `Height`, `CornerRadius`, and the `FromLeft`/`FromRight`/`FromTop` helpers),
never from the raw configuration, which is unscaled. Override `RoundedBodyTop` to `false` for a style
with no body fill (the band then sits flush on a square-topped body, as in `TransparentPluginPanel`).
Override `Draw` itself only for chrome that is not a titled card at all.

## Theming and fonts

`IPluginTheme` is a flat set of `SKColor`s named by role: `Background`,
`BackgroundHighlight`/`BackgroundShadow`, `Accent`/`Accent2`/`AccentDim`,
`TextPrimary`/`TextDim`/`TextDisabled`, `TrackBackground`, the glow triplet (`GlowCore`, `GlowMid`,
`GlowOuter`), the menu colours and the XY-pad colours. `XYLivePoint` is the only member with a default
(`Accent`); everything else must be implemented.

`IMetallicPanelTheme` adds the six metal colours the panels use (the title band too, on every panel
style). Implement one type against both interfaces to theme panels as well; a theme that implements
only `IPluginTheme` falls back to `DefaultMetallicPanelTheme.Instance`.

`DefaultPluginTheme` is the built-in near-monochrome dark palette and the reference for what each slot
is for. It is `sealed`, so start a theme of your own by copying its values rather than deriving from
it.

Set your theme once at startup, before the first window is built. A static bootstrapper keeps that in
one place:

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

…and call `GuiBootstrapper.EnsureInitialized()` at the top of your view's `CreateWindow()`.

`PluginDefaults` holds *defaults*, not the values controls read while drawing: a control always goes
through its window's `RenderContext`, which may carry a theme of its own
(`window.Context.Theme = …`). That indirection is what makes a per-window theme — a host-driven
light/dark switch, say — possible at all. A single control can go one step further with its
configuration's `Theme`, which wins over the window's: a set of controls in a second accent is a
theme that differs only in its accent slots, handed to each of them.

`IPluginFonts` is just `Regular` and `Bold` `SKTypeface`s; controls choose their own sizes. Leave
`PluginDefaults.Fonts` alone to use `DefaultPluginFonts`, which resolves a system font per platform.

## Scaling and design units

`RenderContext` holds a window's `Scale`, `Theme` and `Fonts`. One instance per window, owned by the
window and handed to every control and panel it lays out. Scale deliberately is **not** global: a DAW
routinely opens two editors of the same plugin on monitors with different DPI, and the plugin binary is
loaded once per process — a static scale factor means the last editor to attach wins, for both.

With `PluginView` you never set the scale yourself: it follows the host's DPI/content scale
(multiplied by `UserUiScale`, see [below](#hosting-the-window-in-a-vst-3-plugin)).

Convert with `Rescale(designUnits)` for whole pixels and `RescaleExact(designUnits)` for geometry that
should stay sub-pixel exact (thin strokes, gradient stops). Both are available on `PluginWindow`,
`PluginPanel`, `PluginControl` and `RenderContext` itself.

What is in which unit:

- **Design units**: everything in `BuildLayout()` — `Place` coordinates, explicit width/height,
  `extraWidth`/`extraHeight`, grid pitches, the resulting `ControlPlacement` values — everything in a
  `PluginPanelConfiguration`, every length in a style, and the base size passed to `PluginView`.
- **Pixels**: the canvas and `width`/`height` in `DrawBackground`, a control's
  `_x`/`_y`/`_w`/`_h`/`Bounds`, a panel's scaled accessors, and the coordinates arriving in input
  handlers.

## Writing your own control

Derive from `PluginControl`, take your own configuration in the primary constructor, and override
`Draw` plus whichever input hooks you react to. A matching configuration derives from
`ControlConfiguration` (display-only, commands) or `ParameterControlConfiguration` (parameter-driven);
the smallest one that can be placed without an explicit size implements `ISizedControlConfiguration`:

```csharp
public sealed class DotControlConfiguration : ControlConfiguration, ISizedControlConfiguration
{
    public (int Width, int Height) ResolveBounds() => (24, 24);   // design units
}

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
        if (!IsEnabled) return;   // the base class doesn't filter input for you
        _isOn = !_isOn;
        Refresh();                // request a repaint
    }
}
```

It is placed like any built-in control: `Place(_dot, c => new DotControl(c), 10, 10)`.

Points to keep in mind:

- **Coordinates.** Bounds (`_x`, `_y`, `_w`, `_h`, `Bounds`) are absolute, window-relative and already
  in pixels; `Draw` and the pointer handlers work in **local** coordinates with (0,0) at the control's
  top-left — the canvas and event positions are translated before they arrive. Run any length of your
  own through `Rescale`/`RescaleExact`.
- **Pointer capture.** A press captures the pointer: every move and the release go to the pressed
  control until release, even outside its bounds — so coordinates in `OnPointerMove`/`OnPointerUp` can
  be negative or past `_w`/`_h`. Close gestures (`EndEdit`) in `OnPointerUp`; it always arrives.
- **Reuse.** A control is built once and re-bounded any number of times over its life. Do not assume
  a single layout pass.
- **Repaint.** Call `Refresh()` after changing anything visible. Override `NeedsContinuousRepaint` to
  `true` only for something that animates on its own (as `MeterControl` does) — it puts the whole
  window on a repaint timer (see [Platform notes](#platform-notes) for where that timer exists).
- **Other overrides.** `HitTest(localX, localY)` narrows the clickable area beyond the bounding box
  (the knob restricts it to its disc); `GetParameterInfo` when bound to a parameter — it is what makes
  the host's "what parameter is under this point" query and MIDI-learn menus work;
  `GetContextMenuItems` for a right-click menu (`new ContextMenuItem("Reset to Default", Reset, IsEnabled)`);
  `OnWheel` and `OnDoubleClick`; `OnPointerEnter`/`OnPointerLeave` for hover (`IsHovered` is maintained
  for you — call the base implementation when overriding); and `Dispose` if you cache Skia resources.
- **Menus on a normal click.** `ShowMenu(localX, localY, items)` opens the window's popup menu from
  any handler — that is how the dropdown shows its list. `ContextMenuItem.IsChecked` marks the current
  choice.
- **`SetContainerBackgroundReference`** gives you `_containerW`/`_containerH`, for a control that
  samples the window background to blend against it.

To give a configuration a style of its own: write a style record that implements
`IControlStyle<TStyle>` (a static `Default` and `BoundsFor(size)`, usually read from a
`SizeTable<T>`), and let the configuration implement `IStyledControlConfiguration<TStyle>` by
declaring `Func<TStyle>? Style { get; init; }` — `ResolveBounds()` then comes for free. In the
control, read the style with `config.ResolveStyle()`.

`ControlFactory.Create(config)` (in `TTraxx.PluGui.Gui.Controls.Factories`) builds the matching
built-in control for any built-in configuration — useful when configurations are data-driven. It
throws for configuration types of your own.

## Icons and Skia helpers

`Icons` exposes built-in vector glyphs — `Sine`, `Saw`, `Pulse`, `Triangle` (meant to be stroked) and
`ArrowRight` and `Padlock` (meant to be filled). Every path is built in a normalized box from −0.5 to +0.5 on both
axes, centred on the origin, Y pointing down as in Skia. Draw them through `SkiaIconExtensions`
(`DrawIconFill`, `DrawIconStroke`), which apply the size and position — `x`/`y` there are the icon's
**centre**, in pixels. Passing a path straight to Skia renders a sub-pixel speck at the origin. Paths
are cached and shared process-wide — treat them as read-only and transform a copy, or the canvas.

`SkiaGradientExtensions` covers the fills the built-in controls and panels use:
`FillVerticalGradient` (two colours, or a colour/position array), `FillRoundRectVerticalGradient` and
`FillRoundRectRadialGradient`. `SkiaTextExtensions.DrawTextTopAligned` draws text positioned by its
top edge rather than its baseline, which is what layout code usually wants.

## Hosting the window in a VST 3 plugin

`TTraxx.PluGui.NPlug` (a separate package, namespace `TTraxx.PluGui.NPlug`) implements NPlug's
`IAudioPluginView` — which mirrors the VST 3 `IPlugView` C++ surface — on top of an `IPluginWindow`.
Derive from `PluginView<TWindow>`, pass your editor's base size in design units, and implement one
member, `CreateWindow()`, as in the [quick start](#quick-start-nplug). `CreateWindow()` is called once,
when the host first attaches the view; a host creates a new view each time the editor opens.

Attach/removed, size and size constraints, content scale, parameter hit-testing and teardown are all
handled for you. Everything else is virtual with a working default:

| Member | Default |
| --- | --- |
| `SupportedPlatforms` | HWND and X11. NSView (macOS) is **deliberately left out** while the macOS backend is untested, so a macOS host cannot attach the editor. To try it anyway, override this and add `AudioPluginViewPlatform.NSView` — at your own risk. |
| `UserUiScale` | `1.0` — an extra multiplier on top of the host's DPI/content scale. |
| `MinScale` / `MaxScale` | `0.5` / `4.0`, clamping what a host may request through `SetContentScaleFactor`. |
| `CanResize` | `true`, but `CheckSizeConstraint` forces the host back to the fixed scaled size: the editor is not user-resizable. |
| `OnEditorClosed` | No-op. Called after the window is destroyed. |
| `OnFocus`, `OnWheel`, `OnKeyDown`, `OnKeyUp` | Ignored. |
| `OnRegisterRunLoop` / `OnUnregisterRunLoop` | No-op. **Must be implemented for Linux** — see below. |

`RefreshUI()` repaints to reflect current parameter values — call it from your controller when
parameters change externally (automation, preset load). A derived view can also read `Window` (null
until attached), `Frame` and `Scale`.

### The X11 run loop

X11 has no message loop of its own. A view there must register an fd-based event handler with the
host's run loop — without it the editor opens but never processes input or paint events — and, for
continuously repainting controls such as a meter or an XY pad with live points, a timer. `PluginView`
handles the sequencing (both the window and the host frame must exist, and a host may deliver them in
either order) and then calls `OnRegisterRunLoop(frame, eventPump, timerPump)`. On Windows and macOS it
never fires, because those platform windows report no pump sources.

That last step is a hook rather than library code because the VST 3 Linux run-loop interfaces
(`IAudioPluginRunLoop`, `IAudioPluginLinuxEventHandler`, `IAudioPluginLinuxTimerHandler`) exist in the
NPlug sources but not in the published NPlug package this library builds against. To support Linux,
build your plugin against an NPlug that has them (e.g. NPlug from source) and override the hook:

```csharp
// Inside your PluginView<TWindow> subclass.
private IAudioPluginRunLoop? _runLoop;
private RunLoopEventHandler? _eventHandler;
private RunLoopTimerHandler? _timerHandler;

protected override bool OnRegisterRunLoop(IAudioPluginFrame frame, IEventPumpSource? eventPump, ITimerPumpSource? timerPump)
{
    // Returning false leaves the view unregistered, so a later SetFrame gets another chance.
    if (frame is not IAudioPluginRunLoop runLoop) return false;
    _runLoop = runLoop;

    if (eventPump is not null)
    {
        _eventHandler = new RunLoopEventHandler(eventPump);
        runLoop.RegisterEventHandler(_eventHandler, eventPump.GetPumpHandle());
    }

    if (timerPump is not null)
    {
        _timerHandler = new RunLoopTimerHandler(timerPump);
        runLoop.RegisterTimer(_timerHandler, (ulong)timerPump.PreferredIntervalMilliseconds);
    }

    return true;
}

protected override void OnUnregisterRunLoop()
{
    if (_eventHandler is not null) _runLoop?.UnregisterEventHandler(_eventHandler);
    if (_timerHandler is not null) _runLoop?.UnregisterTimer(_timerHandler);
    _runLoop = null;
    _eventHandler = null;
    _timerHandler = null;
}

private sealed class RunLoopEventHandler(IEventPumpSource pump) : IAudioPluginLinuxEventHandler
{
    public void OnFileDescriptorIsSet(int fileDescriptor) => pump.ProcessPendingEvents();
}

private sealed class RunLoopTimerHandler(ITimerPumpSource pump) : IAudioPluginLinuxTimerHandler
{
    public void OnTimer() => pump.OnTimerTick();
}
```

`timerPump` is only non-null when the layout contains a continuously repainting control at attach
time.

## Hosting the window without NPlug

`PluginWindow` does not know about any plugin SDK; a host adapter drives it through `IPluginWindow`.
`PluginView<TWindow>` is the reference implementation. The sequence it follows:

1. On attach: create the window, set `window.Context.Scale` to `window.GetInitialScaleFactor(parent)`
   (times any extra zoom of your own), and call `AttachToParent(parentHandle, baseWidth, baseHeight)`
   once. `parentHandle` is an HWND, an X11 window id or an `NSView*`.
2. Size the native window with `SetBounds(0, 0, width, height)` in **pixels** (the base size times the
   scale). Call it again whenever the size or scale changes; set `Context.Scale` first.
3. `RefreshUI()` whenever a parameter changes outside the editor; `TryFindParameter(x, y, out id)`
   when the host asks which parameter is under a point.
4. On Linux, poll `EventPumpSource` (watch `GetPumpHandle()`, call `ProcessPendingEvents()`) and, while
   `TimerPumpSource` is non-null, call its `OnTimerTick()` every `PreferredIntervalMilliseconds`.
5. `Destroy()` on detach. A window cannot be re-attached; create a new one.

## Shipping the plugin

**The native Skia library must ship next to your plugin binary.** SkiaSharp is a managed wrapper
around a native library (`libSkiaSharp.dll`, `.so` or `.dylib`). `dotnet publish` puts it in the
publish folder, but not into your plugin binary — including with NativeAOT. Copy it into the same
folder as the plugin binary inside the `.vst3` bundle (for example
`MyPlugin.vst3/Contents/x86_64-win/`). PluGui loads it from the directory of the plugin binary itself,
because a plugin is not the host's main executable and the runtime's default probing looks next to the
DAW instead.

**Recommended: ship it under a private name.** The Windows loader identifies loaded DLLs by file name.
If another Skia-based plugin in the same DAW process has already loaded its own `libSkiaSharp.dll`,
yours silently binds to that one — whatever Skia version it is — which can crash. To prevent this, name
your copy `libSkiaSharp_<PluginBinaryName>.<ext>`, where `<PluginBinaryName>` is the plugin binary's
file name without extension (`MyPlugin.vst3` → `libSkiaSharp_MyPlugin.dll`). PluGui prefers that file
and falls back to the stock name. No code change is needed; this MSBuild target in the plugin's
`.csproj` renames it on publish (it assumes the binary is named after `$(AssemblyName)`):

```xml
<Target Name="PublishSkiaUnderPrivateName" AfterTargets="ComputeResolvedFilesToPublishList">
  <ItemGroup>
    <_StockSkiaAsset Include="@(ResolvedFileToPublish)" Condition="'%(Filename)' == 'libSkiaSharp'" />
    <ResolvedFileToPublish Remove="@(_StockSkiaAsset)" />
    <ResolvedFileToPublish Include="@(_StockSkiaAsset)"
                           RelativePath="libSkiaSharp_$(AssemblyName)%(_StockSkiaAsset.Extension)" />
  </ItemGroup>
</Target>
```

In a JIT-run process such as the harness none of this applies: the runtime's normal probing finds
Skia in the build output.

**NativeAOT.** `TTraxx.PluGui.Gui` is marked `IsAotCompatible` and is reflection-free on the drawing
path, so a plugin built on it can be published with `PublishAot`.

## Platform notes

| | Windows | Linux (X11) | macOS |
| --- | --- | --- | --- |
| Parent handle | HWND | X11 window id | `NSView` |
| Accepted by `PluginView` by default | Yes | Yes | No — deliberately, until verified; opt in via `SupportedPlatforms` |
| Input and paint events | Own message loop | Host run loop, via `OnRegisterRunLoop` | Own run loop |
| Continuous repaint (meter, XY pad live points) | Built in, ~30 Hz | Host run-loop timer, via `OnRegisterRunLoop` | Not implemented — these only update when something else repaints |
| GUI harness | Yes | Yes | No |
| Status | Supported | Supported | Implemented, not yet verified on real hardware |

## Notes on the API surface

- **Public API baseline.** `TTraxx.PluGui.Gui` and `TTraxx.PluGui.NPlug` track their public surface in
  `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`, with `RS0016`/`RS0017` as build **errors**. Those
  two files are also the most precise listing of what is public, if you want to skim the whole surface.
- **AOT.** The GUI package is `IsAotCompatible` and reflection-free on the drawing path. (The harness
  runners use a little reflection as a fallback — they are development-time only and never ship.)
- **Hot Reload.** `GuiHotReload.Reloaded` (in `TTraxx.PluGui.Gui.Helpers.HotReload`) is raised after a
  Hot Reload edit is applied; internal caches for icons and fonts are dropped at the same moment.
  Nothing raises it in a DAW. See the [harness guide](harness.md) for the workflow it belongs to.
- **`Globals.ShowControlBounds`** draws a debug outline around every control. Genuinely process-wide —
  it is a developer's view preference, not a property of a window — and nothing in a shipped plugin
  ever sets it.
