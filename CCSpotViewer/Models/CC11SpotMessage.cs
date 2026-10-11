using Common;

namespace CCSpotViewer.Models;

public class CC11SpotMessage {
    // ── Raw wire fields (f[n] indices) ────────────────────────────────────────

    // f[0]  Always "CC11"
    public string MessageType { get; set; } = string.Empty;

    // f[1]  Frequency in kHz (e.g. 14025.0)
    public double Frequency { get; set; }

    // f[2]  DX station callsign
    public string DxCallsign { get; set; } = string.Empty;

    // f[3]  UTC date string (DD-MMM-YYYY) — kept for display/log only
    public string Date { get; set; } = string.Empty;

    // f[4]  UTC time string (HHMMZ or HHMM) — kept for display/log only
    public string Time { get; set; } = string.Empty;

    // f[5]  Raw comment / skimmer string (e.g. "CW 25 dB 22 WPM CQ")
    public string Comments { get; set; } = string.Empty;

    // f[6]  Spotter callsign; -# suffix stripped by parser
    public string SpotterCallsign { get; set; } = string.Empty;

    // f[7]  Network hops remaining
    public int Hops { get; set; }

    // f[8]  Receiving node (blank for direct RBN spots)
    public string RxViaNode { get; set; } = string.Empty;

    // f[9]  Originating network / feed (e.g. "RBN")
    public string OriginNode { get; set; } = string.Empty;

    // f[10] DX station ITU zone
    public int DxItuZone { get; set; }

    // f[11] DX station CQ zone
    public int DxCqZone { get; set; }

    // f[12] Spotter station ITU zone
    public int SpotterItuZone { get; set; }

    // f[13] Spotter station CQ zone
    public int SpotterCqZone { get; set; }

    // f[14] DX state / province (US/VE only)
    public string DxState { get; set; } = string.Empty;

    // f[15] Spotter state / province (US/VE only)
    public string SpotterState { get; set; } = string.Empty;

    // f[16] DX DXCC country prefix (e.g. "K", "DL", "I")
    public string DxDxccPrefix { get; set; } = string.Empty;

    // f[17] Spotter DXCC country prefix
    public string SpotterDxccPrefix { get; set; } = string.Empty;

    // f[18] DX Maidenhead grid square
    public string DxGrid { get; set; } = string.Empty;

    // f[19] Spotter Maidenhead grid square
    public string SpotterGrid { get; set; } = string.Empty;

    // f[20] Internal CC identifier (e.g. "24", "26")
    public int InternalIdentifier { get; set; }

    // f[21] Coordinates / geolocation offset (e.g. "41.12/-16.87")
    public string Coordinates { get; set; } = string.Empty;

    // f[22] Trailing routing node appended by server
    public string RoutingNode { get; set; } = string.Empty;

    // ── Derived fields ────────────────────────────────────────────────────────

    /// <summary>Frequency in MHz (computed from kHz).</summary>
    public double FrequencyMHz => Frequency / 1000.0;

    /// <summary>
    /// UTC datetime combined from Date + Time strings by the parser.
    /// Null when the time field is blank (some manual spots).
    /// </summary>
    public DateTime? UTC { get; set; }

    /// <summary>
    /// Band code string from BandInfo (e.g. "20", "40", "OUT").
    /// Set by the parser using the injected BandInfo instance.
    /// </summary>
    public string Band { get; set; } = string.Empty;

    /// <summary>
    /// "STN" when RxViaNode is blank AND SpotterCallsign did not end with "-#".
    /// "SKM" for RBN / CW Skimmer / cluster-routed spots.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    // ── Parsed from Comments ──────────────────────────────────────────────────

    /// <summary>Emission mode decoded by the skimmer (e.g. "CW"). Derived from frequency when blank.</summary>
    public string Mode { get; set; } = string.Empty;

    /// <summary>Signal level in dB (0 when not present).</summary>
    public int Db { get; set; }

    /// <summary>Keying speed in WPM (0 when not present).</summary>
    public int Wpm { get; set; }

    /// <summary>Activity keyword after WPM: "CQ", "BEACON", "NCDXF BCN", "TEST", or empty.</summary>
    public string Activity { get; set; } = string.Empty;
}