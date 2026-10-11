using System.Text.Json.Serialization;

namespace DXSpotViewer.Models;

/// <summary>
/// DX spot message received from MQTT
/// ✅ Updated: Removed Needed, Continent, ITUZone; Added RptByType, LocalTime, UTC
/// </summary>
public class DxSpotMessage {
    [JsonPropertyName("type")]
    public string Type { get; set; } = "dx_spot";

    /// <summary>
    /// UTC timestamp from database
    /// </summary>
    [JsonPropertyName("utc")]
    public DateTime UTC { get; set; }

    /// <summary>
    /// Local time (converted from UTC on server)
    /// </summary>
    [JsonPropertyName("localTime")]
    public DateTime LocalTime { get; set; }

    [JsonPropertyName("call")]
    public string Call { get; set; } = "";

    [JsonPropertyName("frequency")]
    public double Frequency { get; set; }

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "";

    [JsonPropertyName("band")]
    public string Band { get; set; } = "";

    [JsonPropertyName("dxcc")]
    public string DXCC { get; set; } = "";

    /// <summary>
    /// CQ Zone of the SPOTTER (not the DX station)
    /// </summary>
    [JsonPropertyName("cqZone")]
    public int CQZone { get; set; }

    /// <summary>
    /// Spotter callsign
    /// </summary>
    [JsonPropertyName("spotter")]
    public string Spotter { get; set; } = "";

    /// <summary>
    /// Report by type (3-letter code, e.g., "DXC", "RBN")
    /// </summary>
    [JsonPropertyName("rptByType")]
    public string RptByType { get; set; } = "";

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = "";

    // ✅ Formatting helpers for display
    public string LocalTimeStr => LocalTime.ToString("HH:mm");
    public string UTCTimeStr => UTC.ToString("HH:mm");
    public string FrequencyDisplay => $"{Frequency:F3}";
}