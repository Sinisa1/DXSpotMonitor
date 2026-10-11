using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CCSpotViewer.Models;
using CCSpotViewer.Services;
using CCSpotViewer.Settings;

namespace CCSpotViewer.ViewModels;

public sealed class MainViewModel:ViewModelBase, IDisposable {
    // ── Service & settings ────────────────────────────────────────────────────
    private readonly CcSpotListenerService _service = new();
    private readonly SettingsCCSpotViewer _settings = new();
    private readonly Dispatcher _dispatcher = Application.Current.Dispatcher;

    // ── Bound collections ─────────────────────────────────────────────────────
    public ObservableCollection<CC11SpotMessage> Spots { get; } = new();

    // ── Log text ──────────────────────────────────────────────────────────────
    private string _appLog = string.Empty;
    private string _errorLog = string.Empty;

    public string AppLog {
        get => _appLog;
        private set => SetField(ref _appLog,value);
    }

    public string ErrorLog {
        get => _errorLog;
        private set => SetField(ref _errorLog,value);
    }

    // ── Connection fields — bound to the GUI TextBoxes ────────────────────────
    private string _host;
    private string _port;
    private string _callsign;

    public string Host {
        get => _host;
        set => SetField(ref _host,value);
    }

    public string Port {
        get => _port;
        set => SetField(ref _port,value);
    }

    public string Callsign {
        get => _callsign;
        set => SetField(ref _callsign,value);
    }

    // ── Connection state ──────────────────────────────────────────────────────
    private bool _isConnected;
    private string _connectButtonLabel = "Connect";

    public bool IsConnected {
        get => _isConnected;
        private set {
            if(!SetField(ref _isConnected,value)) return;
            ConnectButtonLabel = value ? "Disconnect" : "Connect";
        }
    }

    public string ConnectButtonLabel {
        get => _connectButtonLabel;
        private set => SetField(ref _connectButtonLabel,value);
    }

    // ── Commands ──────────────────────────────────────────────────────────────
    public RelayCommand ConnectCommand { get; }
    public RelayCommand ClearSpotsCommand { get; }
    public RelayCommand ClearLogCommand { get; }

    // ── Constructor ───────────────────────────────────────────────────────────
    public MainViewModel() {
        // Seed GUI fields from settings (which loaded from App.config)
        _host     = _settings.CCUserHost;
        _port     = _settings.CCUserPort.ToString();
        _callsign = _settings.CCUserCallsign;

        _service.SpotReceived           += OnSpotReceived;
        _service.MessageLogged          += OnMessageLogged;
        _service.ErrorLogged            += OnErrorLogged;
        _service.ConnectionStateChanged += OnConnectionStateChanged;

        ConnectCommand    = new RelayCommand(_ => ToggleConnect());
        ClearSpotsCommand = new RelayCommand(_ => Spots.Clear());
        ClearLogCommand   = new RelayCommand(_ => { AppLog = string.Empty; ErrorLog = string.Empty; });
    }

    // ── Command handlers ──────────────────────────────────────────────────────
    private void ToggleConnect() {
        if(_service.IsRunning) {
            _service.Stop();
            return;
        }

        if(!int.TryParse(Port,out int port)) {
            AppendError("Invalid port number.");
            return;
        }

        // Push current GUI values into settings before applying
        _settings.CCUserHost     = Host;
        _settings.CCUserPort     = port;
        _settings.CCUserCallsign = Callsign;

        // ApplySettings rebuilds the DB service and copies host/port/callsign to the service
        _service.ApplySettings(_settings);
        _service.Start();
    }

    // ── Service callbacks (thread-pool) ───────────────────────────────────────
    private void OnSpotReceived(CC11SpotMessage spot)
        => _dispatcher.Invoke(() => Spots.Insert(0,spot),DispatcherPriority.Background);

    private void OnMessageLogged(string msg)
        => _dispatcher.Invoke(() => AppendAppLog(msg),DispatcherPriority.Background);

    private void OnErrorLogged(string msg)
        => _dispatcher.Invoke(() => AppendError(msg),DispatcherPriority.Background);

    private void OnConnectionStateChanged(bool connected)
        => _dispatcher.Invoke(() => IsConnected = connected,DispatcherPriority.Background);

    // ── Log helpers ───────────────────────────────────────────────────────────
    private const int MaxLogLines = 500;

    private void AppendAppLog(string msg) {
        var lines = AppLog.Split('\n');
        AppLog = lines.Length > MaxLogLines
            ? string.Join('\n',lines[^MaxLogLines..]) + $"\n[{DateTime.Now:HH:mm:ss}] {msg}"
            : AppLog + $"\n[{DateTime.Now:HH:mm:ss}] {msg}";
    }

    private void AppendError(string msg) {
        var lines = ErrorLog.Split('\n');
        ErrorLog = lines.Length > MaxLogLines
            ? string.Join('\n',lines[^MaxLogLines..]) + $"\n[{DateTime.Now:HH:mm:ss}] {msg}"
            : ErrorLog + $"\n[{DateTime.Now:HH:mm:ss}] {msg}";
    }

    public void Dispose() {
        _service.Stop();
        _service.Dispose();
    }
}