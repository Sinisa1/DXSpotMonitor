#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;

namespace DXSpotViewer.Platforms.Android;

[BroadcastReceiver(
    Enabled = true,
    Exported = true,
    DirectBootAware = true
)]
[IntentFilter(new[] { Intent.ActionBootCompleted,"android.intent.action.QUICKBOOT_POWERON" })]
public class BootReceiver:BroadcastReceiver {
    public override void OnReceive(Context? context,Intent? intent) {
        if(context == null) return;

        System.Diagnostics.Debug.WriteLine("📱 Boot completed - starting MQTT service");

        try {
            var serviceIntent = new Intent(context,typeof(MqttBackgroundService));

            if(Build.VERSION.SdkInt >= BuildVersionCodes.O) {
                context.StartForegroundService(serviceIntent);
            } else {
                context.StartService(serviceIntent);
            }
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"⚠ Boot receiver error: {ex.Message}");
        }
    }
}
#endif