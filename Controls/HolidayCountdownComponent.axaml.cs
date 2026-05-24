using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
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

    private string _displayText = "";

    public string DisplayText
    {
        get => _displayText;
        set
        {
            if (value == _displayText) return;
            _displayText = value;
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

        AttachedToVisualTree += OnAttached;
        DetachedFromVisualTree += OnDetached;
    }

    private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        UpdateContent();
        LessonsService.PostMainTimerTicked += OnTimerTick;
        Settings.PropertyChanged += OnSettingsPropertyChanged;
    }

    private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        LessonsService.PostMainTimerTicked -= OnTimerTick;
        Settings.PropertyChanged -= OnSettingsPropertyChanged;
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateContent();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        UpdateContent();
    }

    private void UpdateContent()
    {
        var now = ExactTimeService.GetCurrentLocalDateTime();
        var next = HolidayService.GetNextHoliday(now);

        if (next == null)
        {
            DisplayText = "暂无节假日数据";
            return;
        }

        var delta = next.Date - now;
        if (delta < TimeSpan.Zero)
            delta = TimeSpan.Zero;

        var fmt = Settings.CustomFormat;
        if (string.IsNullOrWhiteSpace(fmt))
            fmt = "距离 %N 还有 %D天 %H小时%M分";

        DisplayText = fmt
            .Replace("%N", next.Name)
            .Replace("%D", Math.Ceiling(delta.TotalDays).ToString(CultureInfo.InvariantCulture))
            .Replace("%H", Math.Ceiling(delta.TotalHours).ToString(CultureInfo.InvariantCulture))
            .Replace("%M", Math.Ceiling(delta.TotalMinutes).ToString(CultureInfo.InvariantCulture))
            .Replace("%S", Math.Ceiling(delta.TotalSeconds).ToString(CultureInfo.InvariantCulture))
            .Replace("%d", delta.Days.ToString(CultureInfo.InvariantCulture))
            .Replace("%h", delta.Hours.ToString("00", CultureInfo.InvariantCulture))
            .Replace("%m", delta.Minutes.ToString("00", CultureInfo.InvariantCulture))
            .Replace("%s", delta.Seconds.ToString("00", CultureInfo.InvariantCulture));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
