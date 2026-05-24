using System.Text.Json.Serialization;

namespace HolidayCountdown.Models;

public enum DataSourceType
{
    Local = 0,
    GitHub = 1,
    Api = 2,
    Both = 3
}

public class PluginSettings
{
    [JsonPropertyName("dataSource")]
    public DataSourceType DataSource { get; set; } = DataSourceType.Both;

    [JsonPropertyName("gitHubUrl")]
    public string GitHubUrl { get; set; } = "https://raw.githubusercontent.com/SMOPNIM/HolidayCountdown/main/holidays.json";

    [JsonPropertyName("apiUrl")]
    public string ApiUrl { get; set; } = "";

    [JsonPropertyName("lastUpdateTime")]
    public DateTime? LastUpdateTime { get; set; }

    [JsonPropertyName("customHolidays")]
    public List<HolidayInfo> CustomHolidays { get; set; } = new();
}
