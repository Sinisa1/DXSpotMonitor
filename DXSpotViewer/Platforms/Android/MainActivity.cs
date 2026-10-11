using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace DXSpotViewer;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density
)]
public class MainActivity:MauiAppCompatActivity {
    protected override void OnCreate(Bundle? savedInstanceState) {
        base.OnCreate(savedInstanceState);

        // ✅ Request notification permission (Android 13+)
        RequestNotificationPermission();

        // ✅ Start foreground service
        StartMqttService();
    }

    protected override void OnResume() {
        base.OnResume();
        System.Diagnostics.Debug.WriteLine("📱 MainActivity.OnResume()");

        // ✅ Ensure service is running when app comes to foreground
        StartMqttService();
    }

    private void RequestNotificationPermission() {
        if(Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) {
            if(ContextCompat.CheckSelfPermission(this,Android.Manifest.Permission.PostNotifications) != Permission.Granted) {
                ActivityCompat.RequestPermissions(this,new[] { Android.Manifest.Permission.PostNotifications },1001);
            }
        }
    }

    private void StartMqttService() {
        try {
            var intent = new Intent(this,typeof(Platforms.Android.MqttBackgroundService));

            if(Build.VERSION.SdkInt >= BuildVersionCodes.O) {
                StartForegroundService(intent);
            } else {
                StartService(intent);
            }

            System.Diagnostics.Debug.WriteLine("✅ MQTT service started from MainActivity");
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"⚠ Failed to start service: {ex.Message}");
        }
    }
}