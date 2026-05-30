# AGENTS.md — HolidayCountdown

## Build

```bash
dotnet build HolidayCountdown.csproj /p:CreateCipx=true /p:GenerateHashSummary=false
```

- Standalone project — ignore sibling `ClassIsland/global.json` (requires .NET 9). Use .NET 8 SDK.
- CIPX output → `cipx/HolidayCountdown.cipx`. `GenerateHashSummary=false` avoids `pwsh` dependency.
- `manifest.yml` / `icon.png` / `README.md` need `<None Update="..." CopyToOutputDirectory="PreserveNewest" />` (already in `.csproj`).

## Framework

.NET 8 / Avalonia / FluentAvalonia / `Microsoft.Extensions.Hosting`
- NuGet `ClassIsland.PluginSdk` `2.0.0.*` provides `ClassIsland.Core` (controls, services, converters).
- XAML namespace `http://classisland.tech/schemas/xaml/core` for core controls.
- `IClassIslandAppHost` / `IAppHost.TryGetService<T>()` (static) → `ClassIsland.Shared` namespace.
- `ColorToColorPickerBrushConverter` → `ClassIsland.Core.Converters`.

## Architecture

| Component | Pattern | Key file(s) |
|-----------|---------|-------------|
| Entry | `[PluginEntrance]` → `PluginBase` | `Plugin.cs` — manually creates `HolidayService(PluginConfigFolder)`, registers singleton + component + settings |
| Component | `ComponentBase<HolidayCountdownSettings>` + `INotifyPropertyChanged` | `Controls/HolidayCountdownComponent.axaml` + `.cs` |
| Instance settings UI | `ComponentBase<HolidayCountdownSettings>` | `Controls/HolidayCountdownSettingsControl.axaml` + `.cs` |
| Plugin settings page | `SettingsPageBase` + `[SettingsPageInfo(...)]` | `Views/SettingsPages/HolidayDataSettingsPage.axaml` + `.cs` |
| Singleton service | Plain class, ctor takes `string pluginConfigFolder` | `Services/HolidayService.cs` |

## Critical gotchas

- **`$parent[local:...]` in XAML style selectors will crash** — Plugin assemblies load in a custom `AssemblyLoadContext`; `$parent[...]` triggers `Assembly.Load()` which fails. All cross-assembly visual properties must be set in code-behind (`ApplyVisualSettings()`), not XAML setters.
- **Component DataContext** — Must be `{Binding RelativeSource={RelativeSource Mode=FindAncestor, AncestorType=local:HolidayCountdownComponent}}`. Direct binding to the control instance (via `x:Name` or `$parent`) is not reliable across plugin context.
- **`SettingsPageBase` does NOT auto-set DataContext** — Must explicitly add `DataContext="{Binding RelativeSource={RelativeSource Self}}"` in XAML.
- **`new event PropertyChangedEventHandler PropertyChanged`** — Required to suppress CS0108 (hides `AvaloniaObject.PropertyChanged`). Applied to both component and settings models.
- **FontColor stored as string** (`#AARRGGBB` in JSON). Exposed as `Color` via `[JsonIgnore]` `FontColorValue` getter/setter. Two-way ColorPicker binding works through this property.
- **`[JsonPropertyName]` on every serialized property** — System.Text.Json defaults to case-sensitive matching.
- **Two-way ComboBox binding for enums** — Must use a `int` property that casts to/from the enum (see `HolidayDataSettingsPage.DataSourceIndex`). Direct `{Binding DataSource}` with an enum won't round-trip correctly.
- **URLs save via both `TextChanged` AND `LostFocus`** — the settings page calls `SaveDialogSettings()` on both events (see `UrlBox_OnTextChanged` and `UrlBox_OnLostFocus`).
- **Timer lifecycle** — Subscribe/unsubscribe `LessonsService.PostMainTimerTicked` + `Settings.PropertyChanged` inside `AttachedToVisualTree`/`DetachedFromVisualTree`. Never leak subscriptions.
- **API response key mismatch** — `Timor.tech` wraps data under JSON key `"holiday"` (not `"data"`). `TimorTechResponse.Data` needs `[JsonPropertyName("holiday")]`. Each entry also has lowercase fields (`holiday`, `name`, `date`, `target`) — ALL need `[JsonPropertyName]`.
- **API dates via `info.Date`, not dictionary key** — API response keys are `MM-dd` (e.g. `"01-29"`) but the `date` field inside each entry is full `yyyy-MM-dd`. Always use `info.Date` to get the correct year.
- **Remote data replaces full years** — `MergeRemoteHolidays` removes ALL entries whose year falls in the remote data's range, then adds the remote entries. Built-in holidays (including lunar ones not in the API) for those years are lost. User additions via the settings page survive in `custom_holidays.json`.
- **Auto temp layers** — `FindSourcePlanIdForDayOff` finds the target holiday (by `TargetHolidayName` match), then searches class plans for one whose `WeekDay` matches that holiday's `DayOfWeek`. Falls back to scanning the past 7 weekdays.
- **No tests** — No test project exists. Manual verification via ClassIsland plugin load only.
- **HolidayService writes 3 files** to `PluginConfigFolder`: `Settings.json`, `holidays_cache.json`, `custom_holidays.json`.

## API data model (timor.tech)

```json
{"code":0,"holiday":{"01-01":{"holiday":true,"name":"元旦","date":"2025-01-01","target":null}}}
```

`target` is non-null only on 调休补班 entries (`holiday: false`), naming the holiday being compensated (e.g. `"春节"`).

## Commits

- Conventional Commits in Chinese (e.g. `feat:`, `fix:`).
- Remote: `git@github.com:SMOPNIM/HolidayCountdown.git` — `main` branch, push after every change.
