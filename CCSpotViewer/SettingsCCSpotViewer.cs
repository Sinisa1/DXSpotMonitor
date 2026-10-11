using System.Configuration;
using Common;

namespace CCSpotViewer.Settings;

public class SettingsCCSpotViewer:AppSettingsBase {
    // ── CC User listener connection ───────────────────────────────────────────
    public string CCUserHost { get; set; } = "127.0.0.1";
    public int CCUserPort { get; set; } = 7300;
    public string CCUserCallsign { get; set; } = "VE3ZR";

    /// <summary>
    /// Shared BandInfo instance used by the parser to resolve Band from frequency.
    /// Instantiated once here; injected into CC11SpotParser.TryParse().
    /// </summary>
    public BandInfo BandInfo { get; } = new BandInfo();

    public SettingsCCSpotViewer() : base() { }

    public override void InitializeAAAA() {
        // Resolves SQLServer_{Host} and builds SQLServerConnectionString
        SetSQLServer();

        // Load CC User connection defaults from App.config
        CCUserHost     = ConfigurationManager.AppSettings.Get("CCUser_Host")     ?? "127.0.0.1";
        CCUserPort     = int.TryParse(ConfigurationManager.AppSettings.Get("CCUser_Port"),out int p) ? p : 7300;
        CCUserCallsign = ConfigurationManager.AppSettings.Get("CCUser_Callsign") ?? "VE3ZR";
    }
}