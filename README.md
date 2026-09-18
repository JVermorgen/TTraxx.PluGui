# PluGui

<img align="right" width="160px" height="160px" src="https://raw.githubusercontent.com/JVermorgen/TTraxx.PluGui/main/assets/PluGui_250x250.png">

C# VST 3 Gui library for creating cross-platform, SkiaSharp driven audio plugin user interfaces.

The rendering and control framework is plugin-SDK agnostic: it opens a native child window inside
whatever parent handle a host hands it, draws with SkiaSharp, and talks to your plugin purely through
delegates. A thin bridge package implements the VST 3 `IPlugView` surface on top of it for plugins
built with [NPlug](https://github.com/xoofx/NPlug), and a test harness opens that very same
editor outside a DAW so the GUI can be iterated on without a host.

**Currently supported platforms:**
- Windows (HWND)
- Linux (X11)
- macOS (NSView) — implemented, but not yet verified on real hardware. The GUI harness has no macOS
  runner yet, so a macOS editor can currently only be exercised from a real host.

## Packages

All packages target `net10.0` and are published to NuGet.org — tagged releases as stable versions,
every push to `main` as a `-preview.N` prerelease.

| Package | Use it for |
| --- | --- |
| `TTraxx.PluGui.Gui` | The GUI framework itself: windows, controls, panels, theming, platform windowing. No plugin-SDK dependency. |
| `TTraxx.PluGui.NPlug` | VST 3 editor-view base class (`PluginView<TWindow>`) that hosts a PluGui window in an NPlug plugin. |
| `TTraxx.PluGui.Harness.NPlug.Runners` | Cross-platform GUI harness: opens your editor in a standalone window, no DAW required. Development-time only. |
| `TTraxx.PluGui.Harness.NPlug.Runners.Win32` | Windows-only harness runner, pulled in by the package above. |
| `TTraxx.PluGui.Harness.NPlug.Runners.Linux` | Linux/X11-only harness runner, pulled in by the package above. |

```
dotnet add package TTraxx.PluGui.Gui
dotnet add package TTraxx.PluGui.NPlug
dotnet add package TTraxx.PluGui.Harness.NPlug.Runners   # dev-time harness project only
```

The only runtime dependency of the GUI package is SkiaSharp (plus `SkiaSharp.NativeAssets.Linux` for
X11 builds). `TTraxx.PluGui.Gui` is marked `IsAotCompatible` and stays reflection-free on the drawing
path, so a plugin built on it can be published NativeAOT.

## GUI library

[`TTraxx.PluGui.Gui`](https://github.com/JVermorgen/TTraxx.PluGui/blob/main/docs/gui.md) is the part
you write your editor against. You describe a window declaratively — a `BuildLayout()` that yields
placed controls and a `DrawBackground()` that paints everything else — and bind each control to one
host parameter through a `ParameterBinding` of get/set/begin-edit/end-edit delegates. Layout is
written in unscaled design units and rescaled per window, so one layout serves every DPI.

Built in: knobs, toggles, XY pads and level meters, metallic panels, a theme/font abstraction,
right-click context menus, and a `PluginControl` base class for controls of your own.

→ **[Read the GUI guide](https://github.com/JVermorgen/TTraxx.PluGui/blob/main/docs/gui.md)** — mental
model, quick start, every control and configuration, parameter binding, theming, scaling, custom
controls, and the `PluginView<TWindow>` VST 3 bridge.

## GUI harness

[`TTraxx.PluGui.Harness.NPlug.*`](https://github.com/JVermorgen/TTraxx.PluGui/blob/main/docs/harness.md)
is a host stand-in for development. It creates your controller, opens your real editor view in a
native top-level window, and pumps messages until you close it — so you can iterate on layout,
theming and drawing code without loading the plugin into a DAW. It adds a small dev-tool bar
(always-on-top, control-outline overlay) and re-runs your layout on a timer, which makes `dotnet watch`
Hot Reload show edits within a fraction of a second.

→ **[Read the harness guide](https://github.com/JVermorgen/TTraxx.PluGui/blob/main/docs/harness.md)** —
setup, the Hot Reload workflow, dev switches, platform notes and limitations.

## Building from source

```
git clone https://github.com/JVermorgen/TTraxx.PluGui.git
cd TTraxx.PluGui/src
dotnet build TTraxx.PluGui.slnx -c Release
```

`TTraxx.PluGui.Gui` and `TTraxx.PluGui.NPlug` keep a tracked public API baseline
(`PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` next to each `.csproj`). `RS0016`/`RS0017` are
build errors, so adding or removing a public member fails the build until the baseline is updated —
see the comment in `src/Directory.Build.props` for how to record an intentional change.

## License

BSD-3-Clause — see [license.txt](https://github.com/JVermorgen/TTraxx.PluGui/blob/main/license.txt).

## Author

Jasper Vermorgen (TTraxx)
