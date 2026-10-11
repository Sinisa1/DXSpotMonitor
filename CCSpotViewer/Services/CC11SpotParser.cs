using System.Globalization;
using System.Text.RegularExpressions;
using CCSpotViewer.Models;
using Common;

namespace CCSpotViewer.Services;

public static partial class CC11SpotParser {
    // Matches: "CW 25 dB 22 WPM CQ"  /  "CW  5 dB 16 WPM BEACON"  /  "CW 33 dB 28 WPM CQ"
    // Groups: 1=mode  2=dB value  3=WPM value  4=activity (optional)
    [GeneratedRegex(
        @"^(\w+)\s+(\d+)\s+dB\s+(\d+)\s+WPM(?:\s+(.+))?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SkimmerCommentRegex();

    /// <summary>
    /// Parses a raw caret-delimited CC11 line.
    /// <paramref name="bandInfo"/> is used to resolve the band code from frequency.
    /// Returns <c>null</c> for non-CC11 or malformed lines.
    /// </summary>
    public static CC11SpotMessage? TryParse(string line,BandInfo bandInfo) {
        if(string.IsNullOrWhiteSpace(line))
            return null;

        if(!line.StartsWith("CC11^",StringComparison.OrdinalIgnoreCase))
            return null;

        string[] f = line.Split('^');

        // Need at minimum through SpotterCallsign (f[6])
        if(f.Length < 7)
            return null;

        // ── Extract fields needed for derived calculations before object init ─
        string rawComments = f.Length > 5 ? f[5].Trim() : string.Empty;
        string rawSpotter = f.Length > 6 ? f[6].Trim() : string.Empty;
        string rawRxViaNode = f.Length > 8 ? f[8].Trim() : string.Empty;
        string rawDate = f.Length > 3 ? f[3].Trim() : string.Empty;
        string rawTime = f.Length > 4 ? f[4].Trim() : string.Empty;

        double frequency = f.Length > 1 && double.TryParse(f[1],
            NumberStyles.Any,CultureInfo.InvariantCulture,out double freq) ? freq : 0;

        // ── Derived: Type ─────────────────────────────────────────────────────
        bool isSkimmer = !string.IsNullOrEmpty(rawRxViaNode)
                         || rawSpotter.EndsWith("-#",StringComparison.OrdinalIgnoreCase);
        string type = isSkimmer ? "SKM" : "STN";

        // ── Derived: strip -# from spotter callsign ───────────────────────────
        string spotter = rawSpotter.Replace("-#","",StringComparison.OrdinalIgnoreCase);

        // ── Derived: UTC datetime ─────────────────────────────────────────────
        DateTime? utc = Util.CombineToUtcDateTime(rawDate,rawTime);

        // ── Derived: band from frequency (MHz) ────────────────────────────────
        string band = bandInfo.GetBandCodeString(frequency / 1000.0);

        var spot = new CC11SpotMessage {
            MessageType       = f[0],
            Frequency         = frequency,
            DxCallsign        = f.Length > 2 ? f[2].Trim() : string.Empty,
            Date              = rawDate,
            Time              = rawTime,
            Comments          = rawComments,
            SpotterCallsign   = spotter,
            Hops              = f.Length > 7  && int.TryParse(f[7],out int hops) ? hops : 0,
            RxViaNode         = rawRxViaNode,
            OriginNode        = f.Length > 9 ? f[9].Trim() : string.Empty,
            DxItuZone         = f.Length > 10 && int.TryParse(f[10],out int ditu) ? ditu : 0,
            DxCqZone          = f.Length > 11 && int.TryParse(f[11],out int dcq) ? dcq : 0,
            SpotterItuZone    = f.Length > 12 && int.TryParse(f[12],out int situ) ? situ : 0,
            SpotterCqZone     = f.Length > 13 && int.TryParse(f[13],out int scq) ? scq : 0,
            DxState           = f.Length > 14 ? f[14].Trim() : string.Empty,
            SpotterState      = f.Length > 15 ? f[15].Trim() : string.Empty,
            DxDxccPrefix      = f.Length > 16 ? f[16].Trim() : string.Empty,
            SpotterDxccPrefix = f.Length > 17 ? f[17].Trim() : string.Empty,
            DxGrid            = f.Length > 18 ? f[18].Trim() : string.Empty,
            SpotterGrid       = f.Length > 19 ? f[19].Trim() : string.Empty,
            InternalIdentifier= f.Length > 20 && int.TryParse(f[20],out int id) ? id : 0,
            Coordinates       = f.Length > 21 ? f[21].Trim() : string.Empty,
            RoutingNode       = f.Length > 22 ? f[22].Trim() : string.Empty,
            UTC               = utc,
            Band              = band,
            Type              = type,
        };

        // ── Parse skimmer comment: "CW 25 dB 22 WPM CQ" ──────────────────────
        if(!string.IsNullOrEmpty(rawComments)) {
            Match m = SkimmerCommentRegex().Match(rawComments);
            if(m.Success) {
                spot.Mode     = m.Groups[1].Value;
                spot.Db       = int.TryParse(m.Groups[2].Value,out int db) ? db : 0;
                spot.Wpm      = int.TryParse(m.Groups[3].Value,out int wpm) ? wpm : 0;
                spot.Activity = m.Groups[4].Success ? m.Groups[4].Value.Trim() : string.Empty;
            }
        }

        // ── Fallback: derive mode from frequency range when comment has no mode ─
        if(string.IsNullOrEmpty(spot.Mode)) {
            QSOMode modeFromFreq = BandInfo.GetMode(spot.FrequencyMHz);
            spot.Mode = modeFromFreq.ToString();
        }

        return spot;
    }
}