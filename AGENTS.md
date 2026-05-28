# AGENTS.md — HolidayCountdown (ClassIsland Plugin)

## Build

- `dotnet build HolidayCountdown.csproj` — single-project build (no solution). The repo is standalone; ignore `ClassIsland/` sibling's `global.json` (requests .NET 9). Use .NET 8 SDK directly.
- `dotnet build /p:CreateCipx=true /p:GenerateHashSummary=false` — produces `cipx/HolidayCountdown.cipx`. `GenerateHashSummary=false` avoids `pwsh` dependency.
- Install .NET 8 SDK from `dotnet.microsoft.com` if missing.

## Framework

- **.NET 8**, Avalonia + FluentAvalonia, `Microsoft.Extensions.Hosting`
- NuGet: `ClassIsland.PluginSdk` `2.0.0.*` — provides `ClassIsland.Core` (controls, services, converters)
- XAML namespace for core controls: `http://classisland.tech/schemas/xaml/core`

## Plugin Architecture

| Component | File(s) | Pattern |
|-----------|---------|---------|
| Entry | `Plugin.cs` | `[PluginEntrance]` class extending `PluginBase` |
| Component | `Controls/HolidayCountdownComponent.axaml` + `.cs` | `ComponentBase<HolidayCountdownSettings>` + `INotifyPropertyChanged` |
| Per-instance settings | `Controls/HolidayCountdownSettingsControl.axaml` + `.cs` | `ComponentBase<HolidayCountdownSettings>` |
| Plugin-level settings page | `Views/SettingsPages/HolidayDataSettingsPage` | `SettingsPageBase` + `[SettingsPageInfo]` |
| Singleton service | `Services/HolidayService.cs` | Constructed with `PluginConfigFolder` |

## Key Gotchas

- **XAML `$parent[local:...]` breaks at runtime** — Plugin assemblies load in a custom `AssemblyLoadContext`. Style selectors with `$parent[local:...]` call `Assembly.Load()` which fails. Always manage cross-assembly visual properties (colors, visibility) in code-behind, not XAML style setters.
- **`DataContext` not automatic on `SettingsPageBase`** — Must set `DataContext="{Binding RelativeSource={RelativeSource Self}}"` in XAML for `{Binding PropertyOnPage}` to work.
- **`new event PropertyChangedEventHandler PropertyChanged`** — Required to suppress CS0108 (hides `AvaloniaObject.PropertyChanged`).
- **FontColor stored as string** (JSON `#AARRGGBB`), exposed as `Color` via `FontColorValue` property with `[JsonIgnore]`. Two-way ColorPicker binding works through `FontColorValue` setter.
- **Settings models need `[JsonPropertyName]`** on every serialized property.
- **`HolidayService` writes 3 files** to `PluginConfigFolder`: `Settings.json`, `holidays_cache.json`, `custom_holidays.json`.
- **Files in CIPX** (`manifest.yml`, `icon.png`, `README.md`) each need `<None Update="..." CopyToOutputDirectory="PreserveNewest" />` in `.csproj`.

## Commits

- Conventional Commits in Chinese (e.g. `feat:`, `fix:`)
- Remote: `git@github.com:SMOPNIM/HolidayCountdown.git` — `main` branch, push after every change
