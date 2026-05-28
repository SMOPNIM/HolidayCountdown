using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using HolidayCountdown.Models;
using HolidayCountdown.Services;

namespace HolidayCountdown.Controls;

[ComponentInfo("8A1B2C3D-4E5F-6789-AB0C-D1E2F3456789", "假期倒计时", "\uf361", "显示距离下一个法定节假日还有多久。")]
public partial class HolidayCountdownComponent : ComponentBase<HolidayCountdownSettings>, INotifyPropertyChanged
{
    private ILessonsService LessonsService { get; }
    private IExactTimeService ExactTimeService { get; }
    private HolidayService HolidayService { get; }

    private string _holidayName = "";
    private string _daysLeft = "";

    public string HolidayName
    {
        get => _holidayName;
        set
        {
            if (value == _holidayName) return;
            _holidayName = value;
            OnPropertyChanged();
        }
    }

    public string DaysLeft
    {
        get => _daysLeft;
        set
        {
            if (value == _daysLeft) return;
            _daysLeft = value;
            OnPropertyChanged();
        }
    }

    public HolidayCountdownComponent(
        ILessonsService lessonsService,
        IExactTimeService exactTimeService,
        HolidayService holidayService)
    {
        LessonsService = lessonsService;
        ExactTimeService = exactTimeService;
        HolidayService = holidayService;
        InitializeComponent();

        AttachedToVisualTree += (_, _) =>
        {
            UpdateContent();
            ApplyVisualSettings();
            Settings.PropertyChanged += OnSettingsPropertyChanged;
            LessonsService.PostMainTimerTicked += OnTimerTick;
        };
        DetachedFromVisualTree += (_, _) =>
        {
            Settings.PropertyChanged -= OnSettingsPropertyChanged;
            LessonsService.PostMainTimerTicked -= OnTimerTick;
        };
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ApplyVisualSettings();
        UpdateContent();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        UpdateContent();
    }

    private void ApplyVisualSettings()
    {
        var isCompact = Settings.IsCompactModeEnabled;
        TextBlockTo.IsVisible = !isCompact;
        TextBlockConnector.IsVisible = !isCompact;

        if (Settings.IsConnectorColorEmphasized)
        {
            var fontBrush = new SolidColorBrush(Settings.FontColorValue);
            TextBlockTo.Foreground = fontBrush;
            TextBlockConnector.Foreground = fontBrush;
        }
        else
        {
            TextBlockTo.ClearValue(TextBlock.ForegroundProperty);
            TextBlockConnector.ClearValue(TextBlock.ForegroundProperty);
        }
    }

    private void UpdateContent()
    {
        var now = ExactTimeService.GetCurrentLocalDateTime();
        var next = HolidayService.GetNextHoliday(now);

        if (next == null)
        {
            HolidayName = "暂无节假日";
            DaysLeft = "";
            return;
        }

        HolidayName = next.Name;

        var delta = next.Date - now;
        if (delta < TimeSpan.Zero)
            delta = TimeSpan.Zero;

        DaysLeft = Settings.CustomStringFormat
            .Replace("%D", Math.Ceiling(delta.TotalDays).ToString(CultureInfo.InvariantCulture))
            .Replace("%H", Math.Ceiling(delta.TotalHours).ToString(CultureInfo.InvariantCulture))
            .Replace("%M", Math.Ceiling(delta.TotalMinutes).ToString(CultureInfo.InvariantCulture))
            .Replace("%S", Math.Ceiling(delta.TotalSeconds).ToString(CultureInfo.InvariantCulture))
            .Replace("%X", Math.Ceiling(delta.TotalMilliseconds).ToString(CultureInfo.InvariantCulture))
            .Replace("%d", delta.Days.ToString(CultureInfo.InvariantCulture))
            .Replace("%h", delta.Hours.ToString(CultureInfo.InvariantCulture))
            .Replace("%m", delta.Minutes.ToString("00", CultureInfo.InvariantCulture))
            .Replace("%s", delta.Seconds.ToString("00", CultureInfo.InvariantCulture))
            .Replace("%N", next.Name);
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
