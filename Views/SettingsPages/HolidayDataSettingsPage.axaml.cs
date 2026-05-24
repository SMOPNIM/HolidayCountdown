using System.Collections.ObjectModel;
using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
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
        UpdateStatusText();
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
        if (!string.IsNullOrWhiteSpace(GitHubUrlBox.Text))
            HolidayService.Settings.GitHubUrl = GitHubUrlBox.Text;
        if (!string.IsNullOrWhiteSpace(ApiUrlBox.Text))
            HolidayService.Settings.ApiUrl = ApiUrlBox.Text;
        HolidayService.SaveSettings();

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

    private void BtnAddHoliday_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // TODO: 打开添加节假日对话框
    }
}
