using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using FluentAvalonia.UI.Controls;
using HolidayCountdown.Models;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.SettingsPages;

[SettingsPageInfo("holiday.countdown.settings", "法定假日设置", "\ue9b0", "\ue9b0")]
public partial class HolidayDataSettingsPage : SettingsPageBase
{
    private HolidayService HolidayService { get; }

    public ObservableCollection<HolidayInfo> Holidays { get; } = new();

    public int DataSourceIndex
    {
        get => (int)HolidayService.Settings.DataSource;
        set
        {
            HolidayService.Settings.DataSource = (DataSourceType)value;
            HolidayService.SaveSettings();
        }
    }

    public HolidayDataSettingsPage(HolidayService holidayService)
    {
        HolidayService = holidayService;
        InitializeComponent();
        LoadHolidays();
        LoadSettings();
    }

    private void LoadHolidays()
    {
        Holidays.Clear();
        foreach (var h in HolidayService.GetAllHolidaysSorted())
        {
            Holidays.Add(h);
        }
    }

    private void LoadSettings()
    {
        var s = HolidayService.Settings;
        GitHubUrlBox.Text = s.GitHubUrl;
        ApiUrlBox.Text = s.ApiUrl;
        DataSourceCombo.SelectedIndex = (int)s.DataSource;
        UpdateStatusText();
    }

    private void SaveDialogSettings()
    {
        HolidayService.Settings.GitHubUrl = GitHubUrlBox.Text ?? "";
        HolidayService.Settings.ApiUrl = ApiUrlBox.Text ?? "";
        HolidayService.SaveSettings();
    }

    private void UpdateStatusText()
    {
        var lastUpdate = HolidayService.Settings.LastUpdateTime;
        StatusText.Text = lastUpdate.HasValue
            ? $"上次更新：{lastUpdate.Value:yyyy-MM-dd HH:mm}"
            : "尚未从远程更新过数据";
    }

    private async void BtnRefresh_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SaveDialogSettings();

        BtnRefresh.IsEnabled = false;
        BtnRefresh.Content = "更新中…";

        try
        {
            await HolidayService.RefreshFromRemoteAsync();
            LoadHolidays();
            UpdateStatusText();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"更新失败：{ex.Message}";
        }
        finally
        {
            BtnRefresh.IsEnabled = true;
            BtnRefresh.Content = "立即更新";
        }
    }

    private void BtnReset_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        HolidayService.ResetToDefaults();
        LoadHolidays();
        StatusText.Text = "已恢复默认节假日数据";
    }

    private async void BtnAddHoliday_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var nameBox = new TextBox { Watermark = "节日名称" };
        var datePicker = new DatePicker();
        var idBox = new TextBox
        {
            Watermark = "唯一标识（如 spring_festival_2026，留空自动生成）"
        };

        var stack = new StackPanel { Spacing = 12 };
        stack.Children.Add(new TextBlock { Text = "名称", FontWeight = FontWeight.Bold });
        stack.Children.Add(nameBox);
        stack.Children.Add(new TextBlock { Text = "日期", FontWeight = FontWeight.Bold });
        stack.Children.Add(datePicker);
        stack.Children.Add(new TextBlock { Text = "标识", FontWeight = FontWeight.Bold });
        stack.Children.Add(idBox);

        var dialog = new ContentDialog
        {
            Title = "添加节假日",
            Content = stack,
            PrimaryButtonText = "添加",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        var name = nameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusText.Text = "节日名称不能为空";
            return;
        }

        var date = datePicker.SelectedDate?.DateTime ?? DateTime.Now;
        var id = idBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(id))
        {
            id = $"custom_{name}_{date:yyyyMMdd}";
        }

        var holiday = new HolidayInfo
        {
            Id = id,
            Name = name,
            Date = date,
            Description = "用户自定义"
        };

        HolidayService.AddOrUpdateHoliday(holiday);
        LoadHolidays();
        StatusText.Text = $"已添加节假日：{name}";
    }
}
