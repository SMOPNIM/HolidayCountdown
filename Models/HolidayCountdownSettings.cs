using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace HolidayCountdown.Models;

public class HolidayCountdownSettings : INotifyPropertyChanged
{
    private double _fontSize = 14;
    private string _fontColor = "";
    private bool _isCompactMode;
    private bool _showSeconds = true;
    private string _customFormat = "距离 %N 还有 %D天 %H小时%M分";
    private bool _showProgress;

    [JsonPropertyName("fontSize")]
    public double FontSize
    {
        get => _fontSize;
        set => SetField(ref _fontSize, value);
    }

    [JsonPropertyName("fontColor")]
    public string FontColor
    {
        get => _fontColor;
        set => SetField(ref _fontColor, value);
    }

    [JsonPropertyName("isCompactMode")]
    public bool IsCompactMode
    {
        get => _isCompactMode;
        set => SetField(ref _isCompactMode, value);
    }

    [JsonPropertyName("showSeconds")]
    public bool ShowSeconds
    {
        get => _showSeconds;
        set => SetField(ref _showSeconds, value);
    }

    [JsonPropertyName("customFormat")]
    public string CustomFormat
    {
        get => _customFormat;
        set => SetField(ref _customFormat, value);
    }

    [JsonPropertyName("showProgress")]
    public bool ShowProgress
    {
        get => _showProgress;
        set => SetField(ref _showProgress, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
