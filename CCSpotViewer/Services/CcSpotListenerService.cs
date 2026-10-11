using System.IO;
using System.Net.Sockets;
using System.Text;
using CCSpotViewer.Models;
using CCSpotViewer.Settings;
using Common;

namespace CCSpotViewer.Services;

public sealed class CcSpotListenerService:IDisposable {
    // Events fired on the thread-pool — callers must marshal to UI if needed
    public event Action<CC11SpotMessage>? SpotReceived;
    public event Action<string>? MessageLogged;
    public event Action<string>? ErrorLogged;
    public event Action<bool>? ConnectionStateChanged;

    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private RawMessageLogger? _logger;
    private CC11SpotDbService? _dbService;

    // Held so ReadStreamAsync can pass BandInfo to the parser on every call
    private BandInfo _bandInfo = new BandInfo();

    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 7300;
    public string Callsign { get; set; } = "VE3ZR";

    private const int ReconnectDelaySeconds = 5;

    public bool IsRunning => _cts is not null && !_cts.IsCancellationRequested;

    // ── Public control ────────────────────────────────────────────────────────

    /// <summary>
    /// Applies all settings before <see cref="Start"/> is called.
    /// Rebuilds the DB service with the current connection string.
    /// </summary>
    public void ApplySettings(SettingsCCSpotViewer settings) {
        Host        = settings.CCUserHost;
        Port        = settings.CCUserPort;
        Callsign    = settings.CCUserCallsign;
        _bandInfo   = settings.BandInfo;           // shared instance from settings

        _dbService?.Dispose();
        if(!string.IsNullOrWhiteSpace(settings.SQLServerConnectionString)) {
            try {
                _dbService = new CC11SpotDbService(settings.SQLServerConnectionString);
                LogMessage($"DB service ready — {settings.SQLServerConnectionString}");
            } catch(Exception ex) {
                LogError($"Failed to initialise DB service: {ex.Message}");
                _dbService = null;
            }
        } else {
            LogError("SQL connection string is empty — spots will not be saved to DB.");
            _dbService = null;
        }
    }

    public void Start() {
        if(IsRunning) return;

        _logger?.Dispose();
        _logger = new RawMessageLogger();
        LogMessage($"Logging raw messages to: {_logger.FilePath}");

        _cts        = new CancellationTokenSource();
        _listenTask = Task.Run(() => RunLoopAsync(_cts.Token),_cts.Token);
    }

    public void Stop() {
        _cts?.Cancel();
    }

    // ── Internal loop ─────────────────────────────────────────────────────────

    private async Task RunLoopAsync(CancellationToken token) {
        while(!token.IsCancellationRequested) {
            try {
                LogMessage($"Connecting to {Host}:{Port}…");

                using var client = new TcpClient();
                await client.ConnectAsync(Host,Port,token);

                LogMessage("Connected — initialising CC1 mode.");
                ConnectionStateChanged?.Invoke(true);

                using var stream = client.GetStream();
                using var reader = new StreamReader(stream,Encoding.ASCII);
                using var writer = new StreamWriter(stream,Encoding.ASCII) { AutoFlush = true };

                await writer.WriteLineAsync(Callsign.AsMemory(),token);
                await Task.Delay(200,token);
                await writer.WriteLineAsync("SET/VE7CC".AsMemory(),token);
                await Task.Delay(200,token);
                await writer.WriteLineAsync("SET/SKIMMER".AsMemory(),token);
                await Task.Delay(200,token);
                await writer.WriteLineAsync("SET/RBN".AsMemory(),token);

                LogMessage("Initialisation sent — listening for spots.");

                await ReadStreamAsync(reader,token);

                LogMessage("Server closed connection.");
            } catch(OperationCanceledException) {
                break;
            } catch(Exception ex) {
                LogError($"Connection error: {ex.Message}");
            } finally {
                ConnectionStateChanged?.Invoke(false);
            }

            if(!token.IsCancellationRequested) {
                LogMessage($"Reconnecting in {ReconnectDelaySeconds}s…");
                try { await Task.Delay(TimeSpan.FromSeconds(ReconnectDelaySeconds),token); } catch(OperationCanceledException) { break; }
            }
        }

        LogMessage("Listener stopped.");
    }

    private async Task ReadStreamAsync(StreamReader reader,CancellationToken token) {
        while(!token.IsCancellationRequested) {
            string? line = await reader.ReadLineAsync(token);
            if(line is null) break;

            if(line.StartsWith("CC11^",StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("CC1^",StringComparison.OrdinalIgnoreCase)) {
                // 1. Raw log
                if(_logger is not null)
                    await _logger.WriteLineAsync(line);

                // 2. Parse — pass the BandInfo instance from settings
                CC11SpotMessage? spot = CC11SpotParser.TryParse(line,_bandInfo);
                if(spot is null) {
                    LogMessage($"[RAW unparsed] {line}");
                    continue;
                }

                // 3. DB insert — fire-and-forget on thread-pool
                if(_dbService is not null) {
                    CC11SpotDbService dbRef = _dbService;
                    _ = Task.Run(() => {
                        RetCode ret = dbRef.InsertCC11Spot(spot);
                        if(ret.Code != 0)
                            LogError($"DB insert failed — {ret.Msg}");
                    },token);
                }

                // 4. Notify UI
                SpotReceived?.Invoke(spot);
            } else {
                LogMessage($"[NODE] {line}");
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void LogMessage(string msg) => MessageLogged?.Invoke(msg);
    private void LogError(string msg) => ErrorLogged?.Invoke(msg);

    public void Dispose() {
        _cts?.Cancel();
        _cts?.Dispose();
        _logger?.Dispose();
        _dbService?.Dispose();
    }
}