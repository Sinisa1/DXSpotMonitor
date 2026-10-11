using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DXSpotViewer.Models;
using DXSpotViewer.Services;

namespace DXSpotViewer.ViewModels;

public partial class MainViewModel:ObservableObject {
    private MqttService? _mqttService;
    private PreferencesService? _preferencesService;

    [ObservableProperty]
    private string _mqttStatus = "Initializing...";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private int _spotsReceivedCount;

    [ObservableProperty]
    private string _userCall = "";

    [ObservableProperty]
    private DxSpotMessage? _selectedSpot;

    public ObservableCollection<DxSpotMessage> Spots { get; } = new();

    public MainViewModel() {
        System.Diagnostics.Debug.WriteLine("MainViewModel constructor starting...");

        try {
            if(MauiProgram.Services == null) {
                MqttStatus = "App not ready";
                System.Diagnostics.Debug.WriteLine("❌ MauiProgram.Services is null");
                return;
            }

            _mqttService = MauiProgram.Services.GetService<MqttService>();
            _preferencesService = MauiProgram.Services.GetService<PreferencesService>();

            if(_mqttService == null) {
                MqttStatus = "MQTT service unavailable";
                System.Diagnostics.Debug.WriteLine("❌ MqttService is null");
                return;
            }

            if(_preferencesService == null) {
                MqttStatus = "Preferences unavailable";
                System.Diagnostics.Debug.WriteLine("❌ PreferencesService is null");
                return;
            }

            UserCall = _preferencesService.GetUserCall();
            System.Diagnostics.Debug.WriteLine($"UserCall loaded: {UserCall}");

            _mqttService.SpotReceived -= OnSpotReceived;
            _mqttService.ConnectionChanged -= OnConnectionChanged;
            _mqttService.StatusChanged -= OnStatusChanged;

            _mqttService.SpotReceived += OnSpotReceived;
            _mqttService.ConnectionChanged += OnConnectionChanged;
            _mqttService.StatusChanged += OnStatusChanged;

            IsConnected = _mqttService.IsConnected;
            if(_mqttService.IsConnected) {
                MqttStatus = $"Connected as {_mqttService.UserCall}";
                UserCall = _mqttService.UserCall;
            } else {
                MqttStatus = "Not connected";
            }

            _ = EnsureConnectedAsync();

            System.Diagnostics.Debug.WriteLine("✅ MainViewModel initialized successfully");
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"❌ MainViewModel init error: {ex}");
            MqttStatus = "Init error";
        }
    }

    private async Task EnsureConnectedAsync() {
        try {
            await Task.Delay(500);

            if(_mqttService == null || _preferencesService == null) {
                MqttStatus = "Services unavailable";
                return;
            }

            if(_mqttService.IsConnected) {
                MqttStatus = $"Connected as {_mqttService.UserCall}";
                UserCall = _mqttService.UserCall;
                IsConnected = true;
                return;
            }

            if(_preferencesService.HasSavedCredentials()) {
                var userCall = _preferencesService.GetUserCall();
                UserCall = userCall;

                MainThread.BeginInvokeOnMainThread(() =>
                    MqttStatus = $"Connecting as {userCall}...");

                await _mqttService.ConnectAsync(userCall);

#if ANDROID
                StartBackgroundService();
#endif
            } else {
                MainThread.BeginInvokeOnMainThread(() =>
                    MqttStatus = "Not logged in");
            }
        } catch(Exception ex) {
            MainThread.BeginInvokeOnMainThread(() =>
                MqttStatus = "Connection failed");
            System.Diagnostics.Debug.WriteLine($"❌ EnsureConnected error: {ex}");
        }
    }

#if ANDROID
    private void StartBackgroundService() {
        try {
            var context = global::Android.App.Application.Context;
            var intent = new global::Android.Content.Intent(context,typeof(Platforms.Android.MqttBackgroundService));

            if(global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O) {
                context.StartForegroundService(intent);
            } else {
                context.StartService(intent);
            }
            System.Diagnostics.Debug.WriteLine("✅ Background service started");
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"⚠ Background service failed: {ex.Message}");
        }
    }
#endif


    private void OnSpotReceived(object? sender,DxSpotMessage spot) {
        System.Diagnostics.Debug.WriteLine($"📨 SPOT: {spot.Call} {spot.Band} {spot.Mode} RptByType=[{spot.RptByType}]");

        // Determine if alerts should play (with STN check)
        bool shouldPlayAlerts = false;
        bool vibrationEnabled = _preferencesService?.GetVibrationEnabled() ?? true;
        bool soundEnabled = _preferencesService?.GetSoundEnabled() ?? true;
        bool notifyForSTN = _preferencesService?.GetNotifyForSTN() ?? true;
        bool useTts = _preferencesService?.GetUseTts() ?? false;
        float volume = _preferencesService?.GetAlertVolume() ?? 0.7f;

        string rptType = (spot.RptByType ?? "").Trim().ToUpperInvariant();

        if(rptType == "STN") {
            if(notifyForSTN) {
                shouldPlayAlerts = vibrationEnabled || soundEnabled;
                System.Diagnostics.Debug.WriteLine($"  📡 STN spot - alerts ENABLED");
            } else {
                shouldPlayAlerts = false;
                System.Diagnostics.Debug.WriteLine($"  📡 STN spot - alerts DISABLED");
            }
        } else {
            shouldPlayAlerts = vibrationEnabled || soundEnabled;
            System.Diagnostics.Debug.WriteLine($"  📻 Non-STN spot");
        }

        // Play alerts on background thread
        // In OnSpotReceived, where alerts are played:
        if(shouldPlayAlerts) {
            float speed = _preferencesService?.GetSpeechSpeed() ?? 0.8f;
            Task.Run(() => {
                try {
                    if(useTts) {
                        AlertService.PlayAlertsWithTts(
                            vibrationEnabled,soundEnabled,volume,speed,
                            spot.Call ?? "",spot.Band ?? "",spot.Mode ?? "",spot.UTC);
                    } else {
                        AlertService.PlayAlerts(vibrationEnabled,soundEnabled,volume);
                    }
                } catch(Exception ex) {
                    System.Diagnostics.Debug.WriteLine($"❌ Alert error: {ex.Message}");
                }
            });
        }

        // Update UI on main thread
        MainThread.BeginInvokeOnMainThread(() => {
            try {
                bool isTestMessage = spot.Spotter?.Contains("=>TEST") == true;

                if(!isTestMessage) {
                    var age = DateTime.UtcNow - spot.UTC;
                    if(age.TotalMinutes < -5 || age.TotalMinutes > 60) {
                        System.Diagnostics.Debug.WriteLine($"⏭ Old/future rejected");
                        return;
                    }

                    bool isDuplicate = Spots.Any(s =>
                        s.Call == spot.Call &&
                        s.Band == spot.Band &&
                        s.Mode == spot.Mode &&
                        !(s.Spotter?.Contains("=>TEST") == true) &&
                        Math.Abs((s.UTC - spot.UTC).TotalSeconds) < 60);

                    if(isDuplicate) {
                        System.Diagnostics.Debug.WriteLine($"⏭ Duplicate ignored");
                        return;
                    }
                }

                Spots.Insert(0,spot);
                while(Spots.Count > 100) Spots.RemoveAt(Spots.Count - 1);
                SpotsReceivedCount = Spots.Count;
                System.Diagnostics.Debug.WriteLine($"✅ Added: {spot.Call}");
            } catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine($"❌ UI error: {ex.Message}");
            }
        });
    }






    private void OnConnectionChanged(object? sender,bool isConnected) {
        MainThread.BeginInvokeOnMainThread(() => {
            try {
                IsConnected = isConnected;
                if(isConnected && _mqttService != null) {
                    MqttStatus = $"Connected as {_mqttService.UserCall}";
                    UserCall = _mqttService.UserCall;
                } else {
                    MqttStatus = "Disconnected";
                }

                if(!isConnected && _preferencesService?.HasSavedCredentials() == true) {
                    _ = Task.Run(async () => {
                        await Task.Delay(3000);
                        await EnsureConnectedAsync();
                    });
                }
            } catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine($"❌ OnConnectionChanged error: {ex.Message}");
            }
        });
    }

    private void OnStatusChanged(object? sender,string status) {
        MainThread.BeginInvokeOnMainThread(() => {
            try {
                MqttStatus = status;
            } catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine($"❌ OnStatusChanged error: {ex.Message}");
            }
        });
    }

    [RelayCommand]
    private void ClearSpots() {
        Spots.Clear();
        SpotsReceivedCount = 0;
    }

    [RelayCommand]
    private async Task NavigateToSettings() {
        try {
            await Shell.Current.GoToAsync("//SettingsTab");
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"❌ Navigation error: {ex.Message}");
        }
    }
    [RelayCommand]
    private async Task NavigateToInstructions() {
        try {
            await Shell.Current.GoToAsync("//Instructions");
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"❌ Navigation error: {ex.Message}");
        }
    }
}