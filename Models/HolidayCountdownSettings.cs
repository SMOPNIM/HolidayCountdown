using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Avalonia.Media;

namespace HolidayCountdown.Models;

public class HolidayCountdownSettings : INotifyPropertyChanged
{
    private string _fontColor = "#FFFF0000";
    private double _fontSize = 16;
    private bool _isCompactModeEnabled;
    private bool _isConnectorColorEmphasized;
    private string _customStringFormat = "%D天";

    [JsonPropertyName("fontColor")]
    public string FontColor
    {
        get => _fontColor;
        set
        {
            if (SetField(ref _fontColor, value))
                OnPropertyChanged(nameof(FontColorValue));
        }
    }

    [JsonIgnore]
    public Color FontColorValue
    {
        get
        {
            if (Color.TryParse(_fontColor, out var c))
                return c;
            return Colors.Red;
        }
        set => FontColor = value.ToString();
    }

    [JsonPropertyName("fontSize")]
    public double FontSize
    {
        get => _fontSize;
        set => SetField(ref _fontSize, value);
    }

    [JsonPropertyName("isCompactModeEnabled")]
    public bool IsCompactModeEnabled
    {
        get => _isCompactModeEnabled;
        set => SetField(ref _isCompactModeEnabled, value);
    }

    [JsonPropertyName("isConnectorColorEmphasized")]
    public bool IsConnectorColorEmphasized
    {
        get => _isConnectorColorEmphasized;
        set => SetField(ref _isConnectorColorEmphasized, value);
    }

    [JsonPropertyName("customStringFormat")]
    public string CustomStringFormat
    {
        get => _customStringFormat;
        set => SetField(ref _customStringFormat, value);
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
