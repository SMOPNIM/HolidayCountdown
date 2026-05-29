using System.Net.Http.Json;
using System.Text.Json;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Shared;
using HolidayCountdown.Models;

namespace HolidayCountdown.Services;

public class HolidayService
{
    private readonly string _configFolder;
    private readonly string _settingsPath;
    private readonly string _cachePath;
    private readonly string _customHolidaysPath;
    private readonly HttpClient _httpClient;
    private PluginSettings _settings = new();
    private List<HolidayInfo> _holidays = new();
    private List<HolidayInfo> _defaultHolidays;

    public PluginSettings Settings => _settings;

    public IReadOnlyList<HolidayInfo> Holidays => _holidays.AsReadOnly();

    public HolidayService(string pluginConfigFolder)
    {
        _configFolder = pluginConfigFolder;
        _settingsPath = Path.Combine(_configFolder, "Settings.json");
        _cachePath = Path.Combine(_configFolder, "holidays_cache.json");
        _customHolidaysPath = Path.Combine(_configFolder, "custom_holidays.json");
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _defaultHolidays = BuildDefaultHolidays();

        LoadSettings();
        LoadHolidays();
    }

    public HolidayInfo? GetNextHoliday(DateTime now)
    {
        return _holidays
            .Where(h => h.Date > now)
            .OrderBy(h => h.Date)
            .FirstOrDefault();
    }

    public HolidayInfo? GetPreviousHoliday(DateTime now)
    {
        return _holidays
            .Where(h => h.Date < now)
            .OrderByDescending(h => h.Date)
            .FirstOrDefault();
    }

    public List<HolidayInfo> GetAllHolidaysSorted()
    {
        var list = new List<HolidayInfo>(_holidays);
        list.Sort((a, b) => a.Date.CompareTo(b.Date));
        return list;
    }

    public void AddOrUpdateHoliday(HolidayInfo holiday)
    {
        var existing = _holidays.FindIndex(h => h.Id == holiday.Id);
        if (existing >= 0)
            _holidays[existing] = holiday;
        else
            _holidays.Add(holiday);

        SaveCustomHolidays();
    }

    public void RemoveHoliday(string id)
    {
        _holidays.RemoveAll(h => h.Id == id);
        SaveCustomHolidays();
    }

    public async Task RefreshFromRemoteAsync()
    {
        var source = _settings.DataSource;
        List<HolidayInfo>? remoteHolidays = null;

        if (source == DataSourceType.GitHub || source == DataSourceType.Both)
        {
            if (!string.IsNullOrWhiteSpace(_settings.GitHubUrl))
            {
                try
                {
                    remoteHolidays = await FetchFromGitHubAsync();
                }
                catch
                {
                    // fall through
                }
            }
        }

        if (remoteHolidays == null && (source == DataSourceType.Api || source == DataSourceType.Both))
        {
            if (!string.IsNullOrWhiteSpace(_settings.ApiUrl))
            {
                try
                {
                    remoteHolidays = await FetchFromApiAsync();
                }
                catch
                {
                    // fall through
                }
            }
        }

        if (remoteHolidays != null)
        {
            MergeRemoteHolidays(remoteHolidays);
            SaveCache();
            _settings.LastUpdateTime = DateTime.Now;
            SaveSettings();
        }
    }

    private async Task<List<HolidayInfo>?> FetchFromGitHubAsync()
    {
        var response = await _httpClient.GetAsync(_settings.GitHubUrl);
        response.EnsureSuccessStatusCode();
        var wrapper = await response.Content.ReadFromJsonAsync<HolidayDataWrapper>();
        return wrapper?.Holidays;
    }

    private async Task<List<HolidayInfo>?> FetchFromApiAsync()
    {
        var year = DateTime.Now.Year;
        var holidays = new List<HolidayInfo>();

        for (var y = year; y <= year + 1; y++)
        {
            var url = _settings.ApiUrl.TrimEnd('/') + $"/year/{y}";
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
            }
            catch
            {
                continue;
            }

            var json = await response.Content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<TimorTechResponse>(json);
            if (parsed?.Data == null) continue;

            foreach (var (dateStr, info) in parsed.Data)
            {
                if (info == null) continue;
                if (!DateTime.TryParse(dateStr, out var date)) continue;

                var isDayOff = !info.Holiday;
                holidays.Add(new HolidayInfo
                {
                    Id = $"timor_{dateStr}",
                    Name = info.Name ?? "节假日",
                    Date = date,
                    Description = isDayOff ? "调休补班" : "来自 timor.tech API",
                    IsDayOff = isDayOff
                });
            }
        }

        return holidays.Count > 0 ? holidays : null;
    }

    private void MergeRemoteHolidays(List<HolidayInfo> remote)
    {
        foreach (var remoteHoliday in remote)
        {
            var idx = _holidays.FindIndex(h => h.Id == remoteHoliday.Id);
            if (idx >= 0)
                _holidays[idx] = remoteHoliday;
            else
                _holidays.Add(remoteHoliday);
        }
    }

    public void ResetToDefaults()
    {
        _holidays = new List<HolidayInfo>(_defaultHolidays);
        MergeCustomHolidays();
        SaveCache();
    }

    public void SaveSettings()
    {
        var dir = Path.GetDirectoryName(_settingsPath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }

    private void LoadSettings()
    {
        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = File.ReadAllText(_settingsPath);
                _settings = JsonSerializer.Deserialize<PluginSettings>(json) ?? new PluginSettings();
            }
            catch
            {
                _settings = new PluginSettings();
            }
        }
    }

    private void LoadHolidays()
    {
        if (File.Exists(_cachePath))
        {
            try
            {
                var json = File.ReadAllText(_cachePath);
                var cached = JsonSerializer.Deserialize<List<HolidayInfo>>(json);
                if (cached != null && cached.Count > 0)
                {
                    _holidays = cached;
                    MergeCustomHolidays();
                    return;
                }
            }
            catch
            {
                // fall through
            }
        }

        _holidays = new List<HolidayInfo>(_defaultHolidays);
        MergeCustomHolidays();
    }

    private void MergeCustomHolidays()
    {
        if (!File.Exists(_customHolidaysPath))
            return;

        try
        {
            var json = File.ReadAllText(_customHolidaysPath);
            var custom = JsonSerializer.Deserialize<List<HolidayInfo>>(json);
            if (custom == null) return;

            foreach (var h in custom)
            {
                var idx = _holidays.FindIndex(x => x.Id == h.Id);
                if (idx >= 0)
                    _holidays[idx] = h;
                else
                    _holidays.Add(h);
            }
        }
        catch
        {
            // ignore
        }
    }

    private void SaveCustomHolidays()
    {
        var custom = _holidays
            .Where(h => !_defaultHolidays.Any(d => d.Id == h.Id) || HasDifferences(h, _defaultHolidays.First(d => d.Id == h.Id)))
            .ToList();

        var json = JsonSerializer.Serialize(custom, new JsonSerializerOptions { WriteIndented = true });
        var dir = Path.GetDirectoryName(_customHolidaysPath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(_customHolidaysPath, json);
    }

    private static bool HasDifferences(HolidayInfo a, HolidayInfo b)
    {
        return a.Date != b.Date || a.Name != b.Name || a.Description != b.Description || a.IsDayOff != b.IsDayOff;
    }

    private void SaveCache()
    {
        var json = JsonSerializer.Serialize(_holidays, new JsonSerializerOptions { WriteIndented = true });
        var dir = Path.GetDirectoryName(_cachePath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(_cachePath, json);
    }

    private static List<HolidayInfo> BuildDefaultHolidays()
    {
        return new List<HolidayInfo>
        {
            // 2025
            new() { Id = "new_year_2025", Name = "元旦", Date = new DateTime(2025, 1, 1), Description = "1月1日" },
            new() { Id = "spring_festival_2025", Name = "春节", Date = new DateTime(2025, 1, 29), IsLunarBased = true, Description = "农历正月初一" },
            new() { Id = "qingming_2025", Name = "清明节", Date = new DateTime(2025, 4, 4), Description = "4月4日或5日" },
            new() { Id = "labor_2025", Name = "劳动节", Date = new DateTime(2025, 5, 1), Description = "5月1日" },
            new() { Id = "dragon_boat_2025", Name = "端午节", Date = new DateTime(2025, 5, 31), IsLunarBased = true, Description = "农历五月初五" },
            new() { Id = "mid_autumn_2025", Name = "中秋节", Date = new DateTime(2025, 10, 6), IsLunarBased = true, Description = "农历八月十五" },
            new() { Id = "national_day_2025", Name = "国庆节", Date = new DateTime(2025, 10, 1), Description = "10月1日" },
            // 2026
            new() { Id = "new_year_2026", Name = "元旦", Date = new DateTime(2026, 1, 1), Description = "1月1日" },
            new() { Id = "spring_festival_2026", Name = "春节", Date = new DateTime(2026, 2, 17), IsLunarBased = true, Description = "农历正月初一" },
            new() { Id = "qingming_2026", Name = "清明节", Date = new DateTime(2026, 4, 5), Description = "4月4日或5日" },
            new() { Id = "labor_2026", Name = "劳动节", Date = new DateTime(2026, 5, 1), Description = "5月1日" },
            new() { Id = "dragon_boat_2026", Name = "端午节", Date = new DateTime(2026, 6, 19), IsLunarBased = true, Description = "农历五月初五" },
            new() { Id = "mid_autumn_2026", Name = "中秋节", Date = new DateTime(2026, 9, 27), IsLunarBased = true, Description = "农历八月十五" },
            new() { Id = "national_day_2026", Name = "国庆节", Date = new DateTime(2026, 10, 1), Description = "10月1日" },
            // 2027
            new() { Id = "new_year_2027", Name = "元旦", Date = new DateTime(2027, 1, 1), Description = "1月1日" },
            new() { Id = "spring_festival_2027", Name = "春节", Date = new DateTime(2027, 2, 6), IsLunarBased = true, Description = "农历正月初一" },
            new() { Id = "qingming_2027", Name = "清明节", Date = new DateTime(2027, 4, 5), Description = "4月4日或5日" },
            new() { Id = "labor_2027", Name = "劳动节", Date = new DateTime(2027, 5, 1), Description = "5月1日" },
            new() { Id = "dragon_boat_2027", Name = "端午节", Date = new DateTime(2027, 6, 9), IsLunarBased = true, Description = "农历五月初五" },
            new() { Id = "mid_autumn_2027", Name = "中秋节", Date = new DateTime(2027, 9, 15), IsLunarBased = true, Description = "农历八月十五" },
            new() { Id = "national_day_2027", Name = "国庆节", Date = new DateTime(2027, 10, 1), Description = "10月1日" },
            // 2028
            new() { Id = "new_year_2028", Name = "元旦", Date = new DateTime(2028, 1, 1), Description = "1月1日" },
            new() { Id = "spring_festival_2028", Name = "春节", Date = new DateTime(2028, 1, 26), IsLunarBased = true, Description = "农历正月初一" },
            new() { Id = "qingming_2028", Name = "清明节", Date = new DateTime(2028, 4, 4), Description = "4月4日或5日" },
            new() { Id = "labor_2028", Name = "劳动节", Date = new DateTime(2028, 5, 1), Description = "5月1日" },
            new() { Id = "dragon_boat_2028", Name = "端午节", Date = new DateTime(2028, 5, 28), IsLunarBased = true, Description = "农历五月初五" },
            new() { Id = "mid_autumn_2028", Name = "中秋节", Date = new DateTime(2028, 10, 3), IsLunarBased = true, Description = "农历八月十五" },
            new() { Id = "national_day_2028", Name = "国庆节", Date = new DateTime(2028, 10, 1), Description = "10月1日" },
        };
    }

    public void ApplyAutoTempLayers()
    {
        if (!_settings.EnableAutoTempLayer) return;

        var profileService = IAppHost.TryGetService<IProfileService>();
        if (profileService == null) return;

        var today = DateTime.Today;
        var dayOffs = _holidays
            .Where(h => h.IsDayOff && h.Date >= today)
            .ToList();

        if (dayOffs.Count == 0) return;

        foreach (var dayOff in dayOffs)
        {
            Guid? sourceGuid = _settings.DayOffSourcePlanId;
            if (sourceGuid == null || sourceGuid == Guid.Empty)
            {
                sourceGuid = FindSourcePlanId(profileService, dayOff.Date);
            }

            if (sourceGuid != null && sourceGuid != Guid.Empty)
            {
                profileService.CreateTempClassPlan(sourceGuid.Value, enableDateTime: dayOff.Date);
            }
        }
    }

    private static Guid? FindSourcePlanId(IProfileService profileService, DateTime dayOffDate)
    {
        var orderedPlan = profileService.Profile.ClassPlans
            .FirstOrDefault(x => !x.Value.IsOverlay && x.Value.IsEnabled);
        return orderedPlan.Value != null ? orderedPlan.Key : null;
    }

    private class HolidayDataWrapper
    {
        public int Version { get; set; }
        public List<HolidayInfo> Holidays { get; set; } = new();
    }

    private class TimorTechResponse
    {
        public int Code { get; set; }
        public Dictionary<string, TimorTechDayInfo>? Data { get; set; }
    }

    private class TimorTechDayInfo
    {
        public bool Holiday { get; set; }
        public string? Name { get; set; }
        public string? Date { get; set; }
    }
}
