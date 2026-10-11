#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using Android.Net.Wifi;
using AndroidX.Core.App;
using DXSpotViewer.Services;
using Android.Content.PM;

namespace DXSpotViewer.Platforms.Android;

[Service(
    Name = "com.va3tnm.dxspotviewer.MqttBackgroundService",
    ForegroundServiceType = ForegroundService.TypeDataSync,
    Exported = false
)]
public class MqttBackgroundService:Service {
    private const int NotificationId = 9999;
    private const string ChannelId = "dxspot_foreground";
    private const string ChannelName = "DX Spot Monitoring";

    private PowerManager.WakeLock? _wakeLock;
    private WifiManager.WifiLock? _wifiLock;
    private Handler? _keepAliveHandler;
    private Java.Lang.Runnable? _keepAliveRunnable;
    private MqttService? _mqttService;
    private PreferencesService? _preferencesService;
    private bool _isRunning;

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnCreate() {
        base.OnCreate();
        System.Diagnostics.Debug.WriteLine("🚀 MqttBackgroundService.OnCreate()");

        CreateNotificationChannel();
        AcquireWakeLock();
        AcquireWifiLock();
        StartKeepAlive();
    }

    public override StartCommandResult OnStartCommand(Intent? intent,StartCommandFlags flags,int startId) {
        System.Diagnostics.Debug.WriteLine("🚀 MqttBackgroundService.OnStartCommand()");

        if(_isRunning) {
            System.Diagnostics.Debug.WriteLine("  Already running, skipping");
            return StartCommandResult.Sticky;
        }

        _isRunning = true;

        var notification = CreateNotification("Monitoring for DX spots...");
        StartForeground(NotificationId,notification,ForegroundService.TypeDataSync);

        _ = EnsureConnectedAsync();

        System.Diagnostics.Debug.WriteLine("✅ Foreground service started");

        return StartCommandResult.Sticky;
    }

    private async Task EnsureConnectedAsync() {
        try {
            await Task.Delay(1000);

            _mqttService = MauiProgram.Services?.GetService<MqttService>();
            _preferencesService = MauiProgram.Services?.GetService<PreferencesService>();

            if(_mqttService == null || _preferencesService == null) {
                System.Diagnostics.Debug.WriteLine("  ⚠ Services not available yet");
                return;
            }

            if(!_mqttService.IsConnected && _preferencesService.HasSavedCredentials()) {
                var userCall = _preferencesService.GetUserCall();
                System.Diagnostics.Debug.WriteLine($"  🔄 Connecting as {userCall}...");
                await _mqttService.ConnectAsync(userCall);
                UpdateNotification($"Connected as {userCall}");
            }

            _mqttService.ConnectionChanged -= OnConnectionChanged;
            _mqttService.ConnectionChanged += OnConnectionChanged;
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"❌ EnsureConnected error: {ex.Message}");
        }
    }

    private void OnConnectionChanged(object? sender,bool isConnected) {
        if(isConnected) {
            UpdateNotification($"Connected as {_mqttService?.UserCall}");
        } else {
            UpdateNotification("Reconnecting...");
        }
    }

    private void CreateNotificationChannel() {
        if(Build.VERSION.SdkInt >= BuildVersionCodes.O) {
            var channel = new NotificationChannel(ChannelId,ChannelName,NotificationImportance.Low) {
                Description = "Keeps DX Spot monitoring active",
                LockscreenVisibility = NotificationVisibility.Public
            };

            channel.SetSound(null,null);
            channel.EnableVibration(false);

            var manager = (NotificationManager?)GetSystemService(NotificationService);
            manager?.CreateNotificationChannel(channel);

            System.Diagnostics.Debug.WriteLine("✅ Notification channel created");
        }
    }

    private Notification CreateNotification(string text) {
        var intent = new Intent(this,typeof(MainActivity));
        intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

        var pendingIntent = PendingIntent.GetActivity(
            this,0,intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
        );

        return new NotificationCompat.Builder(this,ChannelId)
            .SetContentTitle("DX Spot Viewer")
            .SetContentText(text)
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
            .SetOngoing(true)
            .SetPriority(NotificationCompat.PriorityLow)
            .SetCategory(NotificationCompat.CategoryService)
            .SetContentIntent(pendingIntent)
            .SetForegroundServiceBehavior(NotificationCompat.ForegroundServiceImmediate)
            .Build();
    }

    private void UpdateNotification(string text) {
        try {
            var notification = CreateNotification(text);
            var manager = (NotificationManager?)GetSystemService(NotificationService);
            manager?.Notify(NotificationId,notification);
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"⚠ UpdateNotification error: {ex.Message}");
        }
    }

    private void AcquireWakeLock() {
        try {
            var powerManager = (PowerManager?)GetSystemService(PowerService);
            if(powerManager != null) {
                _wakeLock = powerManager.NewWakeLock(
                    WakeLockFlags.Partial,
                    "DXSpotViewer::MqttWakeLock"
                );
                _wakeLock.Acquire();
                System.Diagnostics.Debug.WriteLine("✅ Wake lock acquired");
            }
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"⚠ Wake lock error: {ex.Message}");
        }
    }

    private void AcquireWifiLock() {
        try {
            var wifiManager = (WifiManager?)GetSystemService(WifiService);
            if(wifiManager != null) {
                // ✅ FIX: Use integer value directly (3 = WIFI_MODE_FULL_HIGH_PERF)
                // WifiMode enum is deprecated/not available in newer bindings
#pragma warning disable CS0618 // Type or member is obsolete
                _wifiLock = wifiManager.CreateWifiLock(
                    (global::Android.Net.WifiMode)3,  // WIFI_MODE_FULL_HIGH_PERF = 3
                    "DXSpotViewer::WifiLock"
                );
#pragma warning restore CS0618
                _wifiLock.Acquire();
                System.Diagnostics.Debug.WriteLine("✅ WiFi lock acquired");
            }
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"⚠ WiFi lock error: {ex.Message}");
        }
    }

    private void StartKeepAlive() {
        _keepAliveHandler = new Handler(Looper.MainLooper!);
        _keepAliveRunnable = new Java.Lang.Runnable(() => {
            _ = CheckAndReconnectAsync();
            _keepAliveHandler?.PostDelayed(_keepAliveRunnable!,30000);
        });
        _keepAliveHandler.PostDelayed(_keepAliveRunnable,30000);
        System.Diagnostics.Debug.WriteLine("✅ Keep-alive timer started");
    }

    private async Task CheckAndReconnectAsync() {
        try {
            if(_mqttService == null || _preferencesService == null) {
                _mqttService = MauiProgram.Services?.GetService<MqttService>();
                _preferencesService = MauiProgram.Services?.GetService<PreferencesService>();
            }

            if(_mqttService != null && !_mqttService.IsConnected) {
                if(_preferencesService?.HasSavedCredentials() == true) {
                    var userCall = _preferencesService.GetUserCall();
                    System.Diagnostics.Debug.WriteLine($"🔄 Keep-alive: Reconnecting as {userCall}...");
                    await _mqttService.ConnectAsync(userCall);
                }
            }
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"⚠ Keep-alive reconnect error: {ex.Message}");
        }
    }

    public override void OnDestroy() {
        System.Diagnostics.Debug.WriteLine("🛑 MqttBackgroundService.OnDestroy()");

        _isRunning = false;

        try { _wakeLock?.Release(); } catch { }
        try { _wifiLock?.Release(); } catch { }

        if(_keepAliveRunnable != null) {
            _keepAliveHandler?.RemoveCallbacks(_keepAliveRunnable);
        }

        if(_mqttService != null) {
            _mqttService.ConnectionChanged -= OnConnectionChanged;
        }

        base.OnDestroy();
    }

    public override void OnTaskRemoved(Intent? rootIntent) {
        System.Diagnostics.Debug.WriteLine("⚠ Task removed - restarting service");

        var restartIntent = new Intent(this,typeof(MqttBackgroundService));
        var pendingIntent = PendingIntent.GetService(
            this,1,restartIntent,
            PendingIntentFlags.OneShot | PendingIntentFlags.Immutable
        );

        if(pendingIntent != null) {
            var alarmManager = (AlarmManager?)GetSystemService(AlarmService);
            alarmManager?.Set(
                AlarmType.ElapsedRealtimeWakeup,
                SystemClock.ElapsedRealtime() + 1000,
                pendingIntent
            );
        }

        base.OnTaskRemoved(rootIntent);
    }
}
#endif