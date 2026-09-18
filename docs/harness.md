# The GUI harness (`TTraxx.PluGui.Harness.NPlug.*`)

A host stand-in for development. It creates your NPlug controller, opens your **real** editor view in
a native top-level window, and pumps messages until you close it — so layout, theming and drawing can
be iterated on without loading the plugin into a DAW. Combined with `dotnet watch`, an edit to a
layout constant or a `Draw` body shows up in the open window within a fraction of a second.

Nothing here ships with a plugin. The harness is a separate executable project that references your
plugin assembly; the packages are development-time only.

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
harness core. The per-platform packages (`...Runners.Win32`, `...Runners.Linux`) exist for a build that
deliberately targets a single OS.

| Type | Namespace |
| --- | --- |
| `HarnessPlugin<TController, TModel, TView>`, `IHarnessPlugin`, `IHarnessRunner`, `HarnessSettingsPanel` | `TTraxx.PluGui.Harness.NPlug.Core` |
| `PlatformHarnessRunnerFactory` | `TTraxx.PluGui.Harness.NPlug.Runners` |
| `Win32HarnessRunner` | `TTraxx.PluGui.Harness.NPlug.Runners.Win32` |
| `XlibHarnessRunner` | `TTraxx.PluGui.Harness.NPlug.Runners.Linux` |

## Quick start

Two files in a console project that references your plugin.

**The harness plugin** — tells the harness which controller, model and view to build:

```csharp
using NPlug;
using TTraxx.PluGui.Harness.NPlug.Core;

internal sealed class MyHarnessPlugin(AudioPluginViewPlatform platform)
    : HarnessPlugin<MyController, MyModel, MyView>(platform)
{
    public override string DisplayName => "My Plugin GUI Harness";
    public override bool AlwaysOnTop => true;          // optional; false by default

    protected override MyView CreateView(MyController controller)
        => new(controller, controller.Model);
}
```

**`Program.cs`** — pick the runner for the current OS and block on it:

```csharp
using TTraxx.PluGui.Gui;
using TTraxx.PluGui.Harness.NPlug.Runners;

// Dev-tool only: draw each control's bounds while iterating on layout.
Globals.ShowControlBounds = args.Contains("--outlines");

var runner = PlatformHarnessRunnerFactory.Create();
runner.Run(new MyHarnessPlugin(runner.Platform));

return 0;
```

```
dotnet run --project src/MyPlugin.Gui.Harness
```

`HarnessPlugin.Create()` constructs the controller, installs a no-op component handler (so
`AudioController.GetHandler()` behaves as it would with a real host), reasserts every parameter to its
declared default through the controller's own edit path, and then calls your `CreateView`. The
defaults step matters because nothing else establishes an initial state: a DAW loads a preset or
saved state right after creating the controller, whereas the harness goes straight to opening the
editor.

`IHarnessRunner.Run` **blocks** until the window closes, so it is effectively the harness's `Main`.

## What a runner actually does

Roughly, in order:

1. Creates a top-level native window, fixed-size, titled with `DisplayName`, optionally
   always-on-top.
2. Creates a plain, undecorated **container** child window offset downward by the dev-tool bar's
   height. This exists only for positioning: a `PluginWindow` always attaches at (0,0) relative to
   whatever parent it is given, so the only way to push the plugin's content below the bar is to give
   it a parent that is already offset.
3. Hands the view a frame (`SetFrame`) and attaches it to the container (`Attached`).
4. Reads back the view's real content scale, rescales the bar to match, resizes the top-level window
   to the plugin's final scaled size plus the bar, and attaches the dev-tool bar to the top-level
   window.
5. Starts a 250 ms timer that calls `RebuildControls()` on the view — fast enough to see a save
   immediately, light enough to leave the CPU alone.
6. Runs the platform message loop; on exit, kills the timer, destroys the bar and calls
   `view.Removed()`.

The runner prefers the typed `IPluGuiPluginView` interface (which `PluginView<TWindow>` implements). If
your view does not implement it, the runner falls back to reflection to find public parameterless
`RebuildControls` / `RefreshUI` methods, and degrades to doing nothing if it finds neither — so a view
written against a different base class still opens, it just will not hot-reload its layout.

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

With the harness window open, save a file. Three things then happen:

1. **The runtime applies the edit.** `TTraxx.PluGui.Gui` registers a `MetadataUpdateHandler`, so caches
   whose builders may have just been rewritten (the `Icons` paths, the built-in fonts) are dropped.
   Register a handler of your own the same way if your plugin caches anything similar.
2. **`GuiHotReload.Reloaded` is raised** (in `TTraxx.PluGui.Gui.Helpers.HotReload`). Subscribe to it if
   you want to react to an edit directly; in a DAW nothing ever raises it.
3. **The runner's 250 ms timer calls `RebuildControls()`**, which re-runs your window's `BuildLayout()`.
   New positions, new panels, and added or removed controls all take effect.

Because controls are cached by their configuration's `Id` and re-bounded rather than recreated, a
rebuild keeps per-control state (drag position, hover) intact, and configurations the rebuild no longer
references are evicted from the cache.

Two consequences worth knowing:

- **Construct configurations once and reuse the instances.** Building fresh configurations on every
  `BuildLayout()` call gives every control a new `Id` each rebuild, which defeats the cache and
  discards control state four times a second.
- **Style factories keep styles live.** A control's `Style` is a `Func<TStyle>`, re-resolved on each
  use, so an edit to a style expression is picked up without a rebuild at all.

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
tag, if you need it outside the runner; note that `runner.Platform` in the quick start already gives
you the same value from the runner you are about to use.

## What the harness is not

- **No audio.** Only the controller, the model and the view are created — no processor, no audio
  thread, no host transport. A meter bound to a real signal will read silence; drive it from a stub
  while iterating.
- **No real host.** `NoOpComponentHandler` swallows parameter edits: automation is not recorded, and
  `CreateContextMenu` and progress reporting throw `NotSupportedException` if something reaches for
  them. Your `BeginEdit`/`EndEdit` calls still execute, they just have nowhere to be written.
- **No preset or state handling.** Parameters start at their declared defaults on every launch.
- **Not a validator.** It exercises the GUI, not VST 3 conformance — keep using an SDK validator for
  that.

## Troubleshooting

**The window opens but nothing repaints on save.** Your view is not reached by `RebuildControls()`.
Confirm it derives from `PluginView<TWindow>` (or otherwise implements `IPluGuiPluginView`), or exposes
a public parameterless `RebuildControls`/`RefreshUI` for the reflection fallback.

**Layout changes appear, colours or icons do not.** Anything cached outside the library needs its own
`MetadataUpdateHandler` to be invalidated — the built-in handler only clears the library's own icon and
font caches.

**Controls flicker or lose their drag state on every tick.** Configurations are being rebuilt rather
than reused; see [the Hot Reload workflow](#the-hot-reload-workflow).

**`PlatformNotSupportedException` at startup.** No runner exists for the current OS — see
[Platform support](#platform-support).

**The editor sits too high, overlapping the bar.** Attach through the runner rather than to the
top-level window directly; the container child window's offset is what positions your editor below the
bar.
