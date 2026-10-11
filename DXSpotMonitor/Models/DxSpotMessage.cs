using System.Text.Json.Serialization;

namespace DXSpotMonitor.Models;

/// <summary>
/// DX spot message for MQTT transmission (matches Android app format)
/// ✅ Updated: Removed Needed, Continent, ITUZone; Added RptByType, LocalTime
/// </summary>
public class DxSpotMessage {
    [JsonPropertyName("type")]
    public string Type { get; set; } = "dx_spot";

    /// <summary>
    /// UTC timestamp from database (not C# generated)
    /// </summary>
    [JsonPropertyName("utc")]
    public DateTime UTC { get; set; }

    /// <summary>
    /// Local time calculated from UTC
    /// </summary>
    [JsonPropertyName("localTime")]
    public DateTime LocalTime => UTC.ToLocalTime();

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

    [JsonPropertyName("cqZone")]
    public int CQZone { get; set; }

    /// <summary>
    /// Spotter callsign (mapped from RptByCall)
    /// </summary>
    [JsonPropertyName("spotter")]
    public string Spotter { get; set; } = "";

    /// <summary>
    /// Report by type (3-letter code)
    /// </summary>
    [JsonPropertyName("rptByType")]
    public string RptByType { get; set; } = "";

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = "";
}