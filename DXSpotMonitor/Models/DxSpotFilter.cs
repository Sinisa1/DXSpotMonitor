using CommunityToolkit.Mvvm.ComponentModel;

namespace DXSpotMonitor.Models;

/// <summary>
/// Represents a DX spot filter for a specific user
/// ✅ Updated for dx schema with UserCall and Mode column
/// </summary>
public partial class DxSpotFilter:ObservableObject {
    private string _originalCallsign = "";
    private string _originalMode = "CW";

    // ✅ NEW: User identification
    [ObservableProperty]
    private string _userCall = "";

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private string _callsign = "";

    // ✅ NEW: Mode as single column ('CW', 'SSB', 'ANY')
    [ObservableProperty]
    private string _mode = "CW";

    // ✅ NEW: Notification period
    [ObservableProperty]
    private int _notifyPeriodMinutes = 30;

    // ✅ NEW: Any band option
    [ObservableProperty]
    private bool _anyBand = true;

    [ObservableProperty]
    private bool _band160m;

    [ObservableProperty]
    private bool _band80m;

    [ObservableProperty]
    private bool _band40m;

    [ObservableProperty]
    private bool _band30m;

    [ObservableProperty]
    private bool _band20m;

    [ObservableProperty]
    private bool _band17m;

    [ObservableProperty]
    private bool _band15m;

    [ObservableProperty]
    private bool _band12m;

    [ObservableProperty]
    private bool _band10m;

    // ✅ NEW: 6m band
    [ObservableProperty]
    private bool _band6m;

    [ObservableProperty]
    private string _cqZones = "4,5";

    public event EventHandler? FilterModified;
    public event EventHandler<CallsignChangedEventArgs>? CallsignChanged;
    public event EventHandler<ModeChangedEventArgs>? ModeChanged;

    public DxSpotFilter() {
        PropertyChanged += (s,e) => {
            if(e.PropertyName == nameof(Callsign)) {
                if(!string.IsNullOrWhiteSpace(_originalCallsign) &&
                    _originalCallsign != Callsign) {
                    CallsignChanged?.Invoke(this,new CallsignChangedEventArgs(_originalCallsign,Callsign));
                }
                _originalCallsign = Callsign;
            } else if(e.PropertyName == nameof(Mode)) {
                if(!string.IsNullOrWhiteSpace(_originalMode) &&
                    _originalMode != Mode) {
                    ModeChanged?.Invoke(this,new ModeChangedEventArgs(_originalMode,Mode));
                }
                _originalMode = Mode;
            }

            FilterModified?.Invoke(this,EventArgs.Empty);
        };
    }

    public string GetOriginalCallsign() {
        return _originalCallsign;
    }

    public string GetOriginalMode() {
        return _originalMode;
    }
    public void SetOriginalCallsign(string callsign) {
        _originalCallsign = callsign;
    }

    public void SetOriginalMode(string mode) {
        _originalMode = mode;
    }

    // ✅ SIMPLIFIED: Mode is now a single string
    public string GetModesForSQL() {
        if(Mode == "ANY") return "'CW','SSB'";
        return $"'{Mode.Replace("'","''")}'";
    }

    public List<int> GetEnabledBands() {
        if(AnyBand) return new List<int> { 160,80,40,30,20,17,15,12,10,6 };

        var bands = new List<int>();
        if(Band160m) bands.Add(160);
        if(Band80m) bands.Add(80);
        if(Band40m) bands.Add(40);
        if(Band30m) bands.Add(30);
        if(Band20m) bands.Add(20);
        if(Band17m) bands.Add(17);
        if(Band15m) bands.Add(15);
        if(Band12m) bands.Add(12);
        if(Band10m) bands.Add(10);
        if(Band6m) bands.Add(6);
        return bands;
    }

    public string GetBandsForSQL() {
        var bands = GetEnabledBands();
        if(!bands.Any()) return "0";
        return string.Join(",",bands);
    }

    public List<int> GetCQZones() {
        try {
            return CqZones
                .Split(',',StringSplitOptions.RemoveEmptyEntries)
                .Select(z => int.Parse(z.Trim()))
                .ToList();
        } catch {
            return new List<int> { 4,5 };
        }
    }

    public string GetCQZonesForSQL() {
        var zones = GetCQZones();
        if(!zones.Any()) return "0";
        return string.Join(",",zones);
    }
}

public class CallsignChangedEventArgs:EventArgs {
    public string OldCallsign { get; }
    public string NewCallsign { get; }

    public CallsignChangedEventArgs(string oldCallsign,string newCallsign) {
        OldCallsign = oldCallsign;
        NewCallsign = newCallsign;
    }
}

// ✅ NEW: Mode changed event
public class ModeChangedEventArgs:EventArgs {
    public string OldMode { get; }
    public string NewMode { get; }

    public ModeChangedEventArgs(string oldMode,string newMode) {
        OldMode = oldMode;
        NewMode = newMode;
    }
}