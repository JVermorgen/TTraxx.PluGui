# The GUI harness (`TTraxx.PluGui.Harness.NPlug.*`)

A host stand-in for development. It creates your NPlug controller, opens your **real** editor view in
a native top-level window, and pumps messages until you close it — so layout, theming and drawing can
be iterated on without loading the plugin into a DAW. Combined with `dotnet watch`, an edit to a
layout constant or a `Draw` body shows up in the open window within a fraction of a second.

Nothing here ships with a plugin. The harness is a separate executable project that references your
plugin project; the packages are development-time only.

- [Packages](#packages)
- [Quick start](#quick-start)
- [What a runner actually does](#what-a-runner-actually-does)
- [The dev-tool bar](#the-dev-tool-bar)
- [The Hot Reload workflow](#the-hot-reload-workflow)
- [Platform support](#platform-support)
- [What the harness is not](#what-the-harness-is-not)
- [Troubleshooting](#troubleshooting)

## Packages

```
dotnet add package TTraxx.PluGui.Harness.NPlug.Runners
```

That one package is all a harness project needs — it pulls in the Win32 and X11 runners and the shared
harness core (`TTraxx.PluGui.Harness.NPlug.Core`, published unlisted because it is never referenced on
its own). The per-platform packages (`...Runners.Win32`, `...Runners.Linux`) exist for a build that
deliberately targets a single OS.

| Type | Namespace |
| --- | --- |
| `HarnessPlugin<TController, TModel, TView>`, `HarnessSettingsPanel` | `TTraxx.PluGui.Harness.NPlug.Core` |
| `IHarnessPlugin`, `IHarnessRunner` | `TTraxx.PluGui.Harness.NPlug.Core.Interfaces` |
| `PlatformHelper` | `TTraxx.PluGui.Harness.NPlug.Core.Helpers` |
| `PlatformHarnessRunnerFactory` | `TTraxx.PluGui.Harness.NPlug.Runners` |
| `Win32HarnessRunner` | `TTraxx.PluGui.Harness.NPlug.Runners.Win32` |
| `XlibHarnessRunner` | `TTraxx.PluGui.Harness.NPlug.Runners.Linux` |

## Quick start

A harness is a small executable project next to your plugin project. Three files.

**The project file** — references your plugin project and the runners package:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- WinExe: no console window next to the editor on Windows. Exe works too. -->
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\MyPlugin\MyPlugin.csproj" />
    <!-- Added by `dotnet add package TTraxx.PluGui.Harness.NPlug.Runners`. Keep its version in
         step with the TTraxx.PluGui.* packages your plugin references. -->
    <PackageReference Include="TTraxx.PluGui.Harness.NPlug.Runners" Version="..." />
  </ItemGroup>

</Project>
```

**The harness plugin** — tells the harness which controller, model and view to build:

```csharp
using NPlug;
using TTraxx.PluGui.Harness.NPlug.Core;

internal sealed class MyHarnessPlugin(AudioPluginViewPlatform platform)
    : HarnessPlugin<MyController, MyModel, MyView>(platform)
{
    public override string DisplayName => "My Plugin GUI Harness";   // window title
    public override bool AlwaysOnTop => true;                        // optional; false by default

    protected override MyView CreateView(MyController controller)
        => new(controller, controller.Model);
}
```

`MyController` needs a public parameterless constructor, as every NPlug controller has. `CreateView`
builds your view exactly as your controller's own `CreateView()` would.

**`Program.cs`** — pick the runner for the current OS and block on it:

```csharp
using TTraxx.PluGui.Gui;
using TTraxx.PluGui.Harness.NPlug.Runners;

// Dev-tool only: start with every control's bounds outlined.
Globals.ShowControlBounds = args.Contains("--outlines");

var runner = PlatformHarnessRunnerFactory.Create();
runner.Run(new MyHarnessPlugin(runner.Platform));

return 0;
```

Run it:

```
dotnet run --project src/MyPlugin.Gui.Harness
dotnet run --project src/MyPlugin.Gui.Harness -- --outlines
```

`HarnessPlugin.Create()` constructs the controller, installs a no-op component handler (so
`AudioController.GetHandler()` behaves as it would with a real host), reasserts every parameter to its
declared default through the controller's own edit path, and then calls your `CreateView`. The
defaults step matters because nothing else establishes an initial state: a DAW loads a preset or
saved state right after creating the controller, whereas the harness goes straight to opening the
editor. Override `Create()` if your controller needs more setup than that.

`IHarnessRunner.Run` **blocks** until the window closes, so it is effectively the harness's `Main`.

## What a runner actually does

Roughly, in order:

1. Creates a top-level native window, fixed-size, titled with `DisplayName`, optionally
   always-on-top.
2. Creates a plain, undecorated **container** child window offset downward by the dev-tool bar's
   height. This exists only for positioning: a `PluginWindow` always attaches at (0,0) relative to
   whatever parent it is given, so the only way to push the plugin's content below the bar is to give
   it a parent that is already offset.
3. Hands the view a frame (`SetFrame`) and attaches it to the container (`Attached`), exactly as a
   host would.
4. Reads back the view's real content scale, rescales the bar to match, resizes the top-level window
   to the plugin's final scaled size plus the bar, and attaches the dev-tool bar to the top-level
   window.
5. Keeps calling `RebuildControls()` on the view, which re-runs your window's `BuildLayout()`: every
   250 ms on Windows (a timer — fast enough to see a save immediately, light enough to leave the CPU
   alone), and on every pass of its polling loop (about every 8 ms) on Linux, where it also pumps the
   editor's X11 events itself.
6. Runs the platform message loop; on exit, stops rebuilding, destroys the bar and calls
   `view.Removed()`.

The runner prefers the typed `IPluGuiPluginView` interface (which `PluginView<TWindow>` implements). If
your view does not implement it, the runner falls back to reflection to find public parameterless
`RebuildControls` / `RefreshUI` methods (every 250 ms on both platforms), and degrades to doing nothing
if it finds neither — so a view written against a different base class still opens, it just will not
hot-reload its layout.

## The dev-tool bar

A fixed-height (50 design units) `HarnessSettingsPanel` is drawn above your editor in every harness
window, with two switches:

| Switch | Effect |
| --- | --- |
| **Always on top** | Toggles the OS-level z-order behaviour of the harness window at runtime. Its initial state comes from `IHarnessPlugin.AlwaysOnTop`. |
| **Show control outlines** | Sets `Globals.ShowControlBounds`, drawing a debug outline around every control's bounds. |

The bar is itself built from the same `PluginWindow`/control framework your editor draws with, so it
automatically picks up the loaded plugin's theme and scales with it. It overrides
`ParticipatesInDebugBoundsOverlay` to `false`, so switching outlines on outlines your controls, not its
own.

`Globals.ShowControlBounds` can also be set before `Run()` — the `--outlines` argument in the quick
start above starts the harness with the overlay already on.

## The Hot Reload workflow

```
dotnet watch --project src/MyPlugin.Gui.Harness run
```

With the harness window open, edit and save a file in your plugin project. Three things then happen:

1. **The runtime applies the edit.** `TTraxx.PluGui.Gui` registers a `MetadataUpdateHandler`, so caches
   whose builders may have just been rewritten (the `Icons` paths, the built-in fonts) are dropped.
   Register a handler of your own the same way if your plugin caches anything similar.
2. **`GuiHotReload.Reloaded` is raised** (in `TTraxx.PluGui.Gui.Helpers.HotReload`). Nothing in the
   library needs it — the runner's rebuild loop picks the edit up anyway — but you can subscribe to
   react to an edit directly. In a DAW nothing ever raises it.
3. **The runner's next rebuild calls `RebuildControls()`**, which re-runs your window's
   `BuildLayout()`. New positions, new panels, and added or removed controls all take effect, and the
   window repaints.

Because controls are cached by their configuration's `Id` and re-bounded rather than recreated, a
rebuild keeps per-control state (drag position, hover) intact, and configurations the rebuild no longer
references are evicted from the cache.

Consequences worth knowing:

- **Construct configurations once and reuse the instances.** Building fresh configurations on every
  `BuildLayout()` call gives every control a new `Id` each rebuild, which defeats the cache and
  discards control state on every rebuild tick — several times a second or more.
- **Style factories keep styles live.** A control's `Style` is a `Func<TStyle>`, re-resolved every time
  the control draws, so an edit to a style expression shows on the next repaint.
- **Configurations themselves are not rebuilt.** They are created once, when your view's
  `CreateWindow()` runs. An edit to a configuration's initializer (a label, a `ControlSize`, a
  `MinValue`) is applied to the code but not to the objects already built; restart the harness to see
  it.

Edits Hot Reload cannot apply (a changed method signature, a new type in some positions) will make
`dotnet watch` restart the process instead; the harness window closes and reopens.

## Platform support

| OS | Runner | Status |
| --- | --- | --- |
| Windows | `Win32HarnessRunner` (direct P/Invoke, per-monitor DPI aware v2) | Supported |
| Linux / X11 | `XlibHarnessRunner` (always-on-top via the EWMH `_NET_WM_STATE` client message) | Supported |
| macOS | — | Not implemented. `PlatformHarnessRunnerFactory.Create()` throws `PlatformNotSupportedException`. |

The GUI library itself does have a Cocoa/NSView backend, so a macOS editor can be exercised from a real
host; it is only the harness that has no macOS runner yet. (That backend is also still unverified on
real hardware — see the [README](../README.md).)

`PlatformHelper.GetCurrentAudioPluginViewPlatform()` maps the running OS onto the VST 3 view platform
tag, if you need it outside the runner; `runner.Platform` in the quick start already gives you the same
value from the runner you are about to use.

## What the harness is not

- **No audio.** Only the controller, the model and the view are created — no processor, no audio
  thread, no host transport. A meter or XY-pad live points fed from the processor will show nothing;
  drive them from a stub while iterating.
- **No host initialization.** The controller is constructed but its `Initialize(host)` is not called,
  and there is no processor to connect to. Setup your controller does there does not happen in the
  harness.
- **No real host.** `NoOpComponentHandler` swallows parameter edits: automation is not recorded, and
  `CreateContextMenu` and progress reporting throw `NotSupportedException` if something reaches for
  them. Your `BeginEdit`/`EndEdit` calls still execute, they just have nowhere to be written.
- **No preset or state handling.** Parameters start at their declared defaults on every launch.
- **No run-loop test.** On Linux the runner pumps the editor's events itself, so it does not exercise
  your `OnRegisterRunLoop` override; check that in a real host.
- **Not a validator.** It exercises the GUI, not VST 3 conformance — keep using an SDK validator for
  that.

## Troubleshooting

**The window opens but nothing changes on save.** Your view is not reached by `RebuildControls()`.
Confirm it derives from `PluginView<TWindow>` (or otherwise implements `IPluGuiPluginView`), or exposes
a public parameterless `RebuildControls`/`RefreshUI` for the reflection fallback. Also check that
`dotnet watch` is watching the harness project (it follows project references into your plugin).

**Layout changes appear, a label or size change does not.** That value lives in a configuration, which
is only built once — see [the Hot Reload workflow](#the-hot-reload-workflow). Restart the harness.

**Layout changes appear, colours or icons do not.** Anything cached outside the library needs its own
`MetadataUpdateHandler` to be invalidated — the built-in handler only clears the library's own icon and
font caches.

**Controls flicker or lose their drag state on every tick.** Configurations are being rebuilt rather
than reused; see [the Hot Reload workflow](#the-hot-reload-workflow).

**Something in the editor throws, but the same code works in a DAW.** Look for code that relies on the
host: `Initialize(host)` having run, a processor sending data, or the component handler's context-menu
or progress support — see [What the harness is not](#what-the-harness-is-not).

**`PlatformNotSupportedException` at startup.** No runner exists for the current OS — see
[Platform support](#platform-support).
