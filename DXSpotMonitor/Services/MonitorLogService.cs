using MQTTnet.Server;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows.Media;

namespace DXSpotMonitor.Services;

/// <summary>
/// Log entry for screen display - ONLY SENT SPOTS
/// </summary>
public class LogEntry {
    public DateTime LocalTime { get; set; }
    public DateTime? UtcTime { get; set; }
    public string Message { get; set; } = "";
    public string? TargetUser { get; set; }
    public string? DxCall { get; set; }
    public string? Band { get; set; }
    public string? Mode { get; set; }
    public double? Frequency { get; set; }
    public string? Spotter { get; set; }
    public string? RptByType { get; set; }
    public string? Comment { get; set; }

    public string LocalTimeStr => LocalTime.ToString("HH:mm");
    public string UtcTimeStr => UtcTime?.ToString("HH:mm") ?? "";
    public string FrequencyStr => Frequency?.ToString("F3") ?? "";

    // Sent spots are highlighted
    public Brush? RowBackground { get; set; } //=> new SolidColorBrush(Color.FromRgb(39,174,96)); // Green
    public Brush? TextColor { get; set; }//=> Brushes.White;
}

/// <summary>
/// Detailed logging service for DX Spot Monitor
/// - File logging: Everything
/// - Screen grid: Only SENT spots
/// - Header text: Timer ticks and summaries
/// </summary>
public class MonitorLogService:IDisposable {
    private readonly string _logFilePath;
    private readonly object _lockObject = new();
    private StreamWriter? _writer;
    private readonly DateTime _sessionStartTime;
    private bool _isFileLoggingEnabled = true;

    public event EventHandler<string>? LastDecisionChanged;
    public event EventHandler<string>? ActivityHeaderChanged;

    public string LastDecision { get; private set; } = "";
    public DateTime LastDecisionTime { get; private set; }
    public string LogFilePath => _logFilePath;
    public bool IsFileLoggingEnabled => _isFileLoggingEnabled;

    // Screen log entries - ONLY SENT SPOTS
    public ObservableCollection<LogEntry> ScreenLogEntries { get; } = new();

    private const int MaxScreenLogEntries = 100; // Keep last 100 sent spots

    public MonitorLogService() {
        _sessionStartTime = DateTime.Now;

        var logDirectory = @"C:\temp";
        if(!Directory.Exists(logDirectory)) {
            Directory.CreateDirectory(logDirectory);
        }

        var fileName = $"DXSpotMonitor_{_sessionStartTime:yyyy-MM-dd_HHmmss}.log";
        _logFilePath = Path.Combine(logDirectory,fileName);

        InitializeLogFile();
    }

    private void InitializeLogFile() {
        try {
            _writer = new StreamWriter(_logFilePath,append: false,encoding: Encoding.UTF8) {
                AutoFlush = true
            };

            WriteHeader();
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to create log file: {ex.Message}");
        }
    }

    private void WriteHeader() {
        var header = $"""
            ╔══════════════════════════════════════════════════════════════════════════════╗
            ║                      DX SPOT MONITOR - SESSION LOG                           ║
            ╠══════════════════════════════════════════════════════════════════════════════╣
            ║  Session Started: {_sessionStartTime:yyyy-MM-dd HH:mm:ss} (Local Time)
            ║  Log File: {_logFilePath}
            ║  Computer: {Environment.MachineName}
            ║  User: {Environment.UserName}
            ╚══════════════════════════════════════════════════════════════════════════════╝

            """;

        _writer?.WriteLine(header);
    }

    public void StopFileLogging() {
        if(_isFileLoggingEnabled) {
            Log("FILE LOGGING STOPPED BY USER");
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
            _isFileLoggingEnabled = false;
        }
    }

    public void Log(string message) {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logLine = $"[{timestamp}] {message}";

        if(_isFileLoggingEnabled) {
            lock(_lockObject) {
                _writer?.WriteLine(logLine);
            }
        }

        System.Diagnostics.Debug.WriteLine(logLine);
    }

    /// <summary>
    /// Update the activity header text (for timer ticks/summaries)
    /// </summary>
    private void UpdateActivityHeader(string text) {
        ActivityHeaderChanged?.Invoke(this,text);
    }

    /// <summary>
    /// Add a SENT SPOT to the screen grid
    /// </summary>
    private void AddSentSpotToGrid(string targetUser,string dxCall,string band,string mode,
        double frequency,string spotter,string rptByType,DateTime utcTime, string comment) {
        var entry = new LogEntry {
            LocalTime = DateTime.Now,
            UtcTime = utcTime,
            Message = $"→ {targetUser}",
            TargetUser = targetUser,
            DxCall = dxCall,
            Band = band,
            Mode = mode,
            Frequency = frequency,
            Spotter = spotter,
            RptByType=rptByType,
            Comment=comment
        };
        if(entry.RptByType=="SKM") {
            entry.RowBackground=  Brushes.DarkGreen;
            entry.TextColor=  Brushes.White;
        } else {
            entry.RowBackground=  new SolidColorBrush(Color.FromRgb(240,0,0));       // Brushes.PaleVioletRed;
            entry.TextColor=  new SolidColorBrush(Color.FromRgb(240,240,240));

        }
        System.Windows.Application.Current?.Dispatcher.Invoke(() => {
            // Insert at top (newest first)
            ScreenLogEntries.Insert(0,entry);

            // Keep only last N entries
            while(ScreenLogEntries.Count > MaxScreenLogEntries) {
                ScreenLogEntries.RemoveAt(ScreenLogEntries.Count - 1);
            }
        });
    }

    public void LogSection(string title) {
        var separator = new string('═',70);
        Log($"");
        Log($"╔{separator}╗");
        Log($"║ {title.PadRight(68)} ║");
        Log($"╚{separator}╝");
    }

    public void LogSubSection(string title) {
        Log($"");
        Log($"─── {title} ───");
    }

    public void LogTimerTick(int intervalSeconds,int activeUsers,int totalFilters) {
        LogSection($"TIMER TICK - Check Interval: {intervalSeconds}s");
        Log($"  Active Users: {activeUsers}");
        Log($"  Total Filters: {totalFilters}");
        Log($"  UTC Time: {DateTime.UtcNow:HH:mm:ss}");
        Log($"  Local Time: {DateTime.Now:HH:mm:ss}");

        // ✅ Update header instead of grid
        UpdateActivityHeader($"📜 Checking... [{DateTime.Now:HH:mm:ss}] - {totalFilters} filters, {activeUsers} users");
    }

    public void LogCheckNowTriggered() {
        LogSection("CHECK NOW - Manual Trigger");
        Log($"  UTC Time: {DateTime.UtcNow:HH:mm:ss}");
        Log($"  Local Time: {DateTime.Now:HH:mm:ss}");

        UpdateActivityHeader($"📜 Manual check [{DateTime.Now:HH:mm:ss}]...");
    }

    public void LogUserProcessingStart(string userCall,int filterCount) {
        LogSubSection($"Processing User: {userCall} ({filterCount} filter(s))");
    }

    public void LogFilterDetails(
        string userCall,string dxCall,string mode,int notifyPeriodMinutes,
        bool anyBand,string bands,string cqZones,bool isEnabled) {
        Log($"");
        Log($"  📋 FILTER: {userCall}/{dxCall}/{mode}");
        Log($"     ├─ Enabled: {isEnabled}");
        Log($"     ├─ Notify Period: {notifyPeriodMinutes} minutes");
        Log($"     ├─ Bands: {(anyBand ? "ANY" : bands)}");
        Log($"     └─ CQ Zones: {(string.IsNullOrEmpty(cqZones) ? "ANY" : cqZones)}");
    }

    public void LogQueryParameters(DateTime sinceTime,int notifyPeriodMinutes,DateTime filterLastCheck) {
        Log($"     🔍 QUERY PARAMETERS:");
        Log($"        ├─ Filter Last Check: {filterLastCheck:HH:mm:ss} UTC");
        Log($"        ├─ Oldest Allowed: {DateTime.UtcNow.AddMinutes(-notifyPeriodMinutes):HH:mm:ss} UTC");
        Log($"        ├─ Query Since: {sinceTime:HH:mm:ss} UTC");
        Log($"        └─ Lookback Window: {notifyPeriodMinutes} minutes");
    }

    public void LogSpotsFound(int count,string dxCall) {
        Log($"     📡 SPOTS FOUND: {count} for {dxCall}");
    }

    public void LogSpotEvaluation(string userCall,string dxCall,string band,string mode,
        DateTime spotTimestamp,string spotter) {
        Log($"");
        Log($"     ⚡ EVALUATING SPOT:");
        Log($"        ├─ DX Call: {dxCall}");
        Log($"        ├─ Band: {band}");
        Log($"        ├─ Mode: {mode}");
        Log($"        ├─ Timestamp: {spotTimestamp:HH:mm:ss} UTC");
        Log($"        └─ Spotter: {spotter}");
    }

    public void LogDecision(string userCall,string dxCall,string band,string mode,
        bool willSend,string reason) {
        var localTime = DateTime.Now;
        var utcTime = DateTime.UtcNow;
        var decision = willSend
            ? $"✅ SEND: {dxCall} on {band} {mode} → {userCall}"
            : $"⏭ SKIP: {dxCall} on {band} {mode} → {userCall}";

        var fullDecision = $"{decision} | Reason: {reason}";

        Log($"");
        Log($"     📝 DECISION [{localTime:HH:mm:ss} Local / {utcTime:HH:mm:ss} UTC]:");
        Log($"        ├─ Action: {(willSend ? "SEND NOTIFICATION" : "DO NOT SEND")}");
        Log($"        ├─ User: {userCall}");
        Log($"        ├─ DX Call: {dxCall}");
        Log($"        ├─ Band/Mode: {band} {mode}");
        Log($"        └─ Reason: {reason}");

        LastDecision = fullDecision;
        LastDecisionTime = localTime;
        LastDecisionChanged?.Invoke(this,fullDecision);
    }

    /// <summary>
    /// ✅ Log SENT SPOT - This is the ONLY thing that goes to the screen grid
    /// </summary>
    public void LogSpotSent(string userCall,string dxCall,string band,string mode,
        double frequency,string spotter,string rptByType,DateTime spotUtcTime,string comment) {
        var localTime = DateTime.Now;
        var utcTime = DateTime.UtcNow;

        // File log - detailed
        Log($"");
        Log($"     ════════════════════════════════════════════════════════════");
        Log($"     ✅ SPOT SENT TO USER");
        Log($"     ════════════════════════════════════════════════════════════");
        Log($"        Target User: {userCall}");
        Log($"        DX Call: {dxCall}");
        Log($"        Frequency: {frequency:F3} MHz");
        Log($"        Band: {band}");
        Log($"        Mode: {mode}");
        Log($"        Spotter: {spotter}");
        Log($"        RptByType: {rptByType}");
        Log($"        Spot UTC: {spotUtcTime:HH:mm:ss}");
        Log($"        Sent Local: {localTime:HH:mm:ss}");
        Log($"        Sent UTC: {utcTime:HH:mm:ss}");
        Log($"     ════════════════════════════════════════════════════════════");

        // ✅ Add to screen grid - ONLY sent spots
        AddSentSpotToGrid(userCall,dxCall,band,mode,frequency,spotter, rptByType,spotUtcTime, comment);

        // Update decision display
        var decision = $"✅ SENT: {dxCall} @ {frequency:F3} MHz ({band} {mode}) → {userCall}";
        LastDecision = decision;
        LastDecisionTime = localTime;
        LastDecisionChanged?.Invoke(this,decision);
    }

    public void LogDuplicateInBatch(string dxCall,string spotKey) {
        Log($"     ⏭ Duplicate in batch: {dxCall} (key: {spotKey})");
    }

    public void LogAlreadySentRecently(string userCall,string dxCall,string band,string mode,int withinMinutes) {
        Log($"     ⏭ Already sent to {userCall} within {withinMinutes} min: {dxCall} {band} {mode}");
    }

    public void LogFilterSkipped(string userCall,string dxCall,string mode,string reason) {
        Log($"");
        Log($"     ⚠ FILTER SKIPPED: {userCall}/{dxCall}/{mode}");
        Log($"        └─ Reason: {reason}");
    }

    public void LogCheckSummary(int totalFound,int totalSent,int usersChecked,int filtersChecked) {
        Log($"");
        LogSection("CHECK CYCLE SUMMARY");
        Log($"  Users Processed: {usersChecked}");
        Log($"  Filters Evaluated: {filtersChecked}");
        Log($"  Spots Found: {totalFound}");
        Log($"  Notifications Sent: {totalSent}");
        Log($"  Completion Time: {DateTime.Now:HH:mm:ss} (Local)");

        // ✅ Update header instead of grid
        var sentText = totalSent > 0 ? $"✅ Sent {totalSent}" : "No new spots";
        UpdateActivityHeader($"📜 Sent Spots [{DateTime.Now:HH:mm:ss}] - {sentText} (Found {totalFound}, {filtersChecked} filters)");
    }

    public void LogMqttStatus(bool isConnected,string message) {
        Log($"");
        Log($"  🔌 MQTT: {(isConnected ? "CONNECTED" : "DISCONNECTED")} - {message}");
    }

    public void LogMonitoringStarted(int intervalSeconds,int activeUsers,int totalFilters) {
        LogSection("MONITORING STARTED");
        Log($"  Check Interval: {intervalSeconds} seconds");
        Log($"  Active Users: {activeUsers}");
        Log($"  Total Filters: {totalFilters}");
        Log($"  Start Time: {DateTime.Now:HH:mm:ss} (Local)");

        UpdateActivityHeader($"📜 Monitoring STARTED [{DateTime.Now:HH:mm:ss}] - {totalFilters} filters, {intervalSeconds}s interval");
    }

    public void LogMonitoringStopped() {
        LogSection("MONITORING STOPPED");
        Log($"  Stop Time: {DateTime.Now:HH:mm:ss} (Local)");

        UpdateActivityHeader($"📜 Monitoring STOPPED [{DateTime.Now:HH:mm:ss}]");
    }

    public void LogError(string context,string message) {
        Log($"");
        Log($"  ❌ ERROR in {context}: {message}");
        UpdateActivityHeader($"📜 ❌ ERROR: {context} [{DateTime.Now:HH:mm:ss}]");
    }

    public void LogError(string context,Exception ex) {
        Log($"");
        Log($"  ❌ ERROR in {context}:");
        Log($"     Message: {ex.Message}");
        Log($"     Stack: {ex.StackTrace}");
        UpdateActivityHeader($"📜 ❌ ERROR: {context} [{DateTime.Now:HH:mm:ss}]");
    }

    public void LogFilterAdded(string userCall,string dxCall,string mode) {
        Log($"");
        Log($"  ➕ FILTER ADDED: {userCall}/{dxCall}/{mode} at {DateTime.Now:HH:mm:ss}");
    }

    public void LogFilterDeleted(string userCall,string dxCall,string mode) {
        Log($"");
        Log($"  🗑 FILTER DELETED: {userCall}/{dxCall}/{mode} at {DateTime.Now:HH:mm:ss}");
    }

    public void LogFilterModified(string userCall,string dxCall,string mode,string property,string oldValue,string newValue) {
        Log($"");
        Log($"  ✏ FILTER MODIFIED: {userCall}/{dxCall}/{mode} - {property}: {oldValue} → {newValue}");
    }

    public void LogStartupAutoConnect() {
        LogSection("AUTO-STARTUP SEQUENCE");
        Log($"  Initiating automatic connection to MQTT broker...");
        UpdateActivityHeader("📜 Starting up - Connecting to MQTT...");
    }

    public void LogStartupAutoMonitor() {
        Log($"  Initiating automatic monitoring...");
    }

    public void Dispose() {
        if(_isFileLoggingEnabled) {
            Log($"");
            LogSection("SESSION ENDED");
            Log($"  End Time: {DateTime.Now:HH:mm:ss} (Local)");
            Log($"  Duration: {DateTime.Now - _sessionStartTime:hh\\:mm\\:ss}");

            _writer?.Flush();
            _writer?.Dispose();
        }
    }
}