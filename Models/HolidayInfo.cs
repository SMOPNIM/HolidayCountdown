using System.Text.Json.Serialization;

namespace HolidayCountdown.Models;

public class HolidayInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("isLunarBased")]
    public bool IsLunarBased { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
