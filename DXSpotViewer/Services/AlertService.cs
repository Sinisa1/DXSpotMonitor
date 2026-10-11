#if ANDROID
using Android.Media;
#endif

namespace DXSpotViewer.Services;

/// <summary>
/// Centralized alert service for vibration, sound, and TTS
/// </summary>
public static class AlertService {

    /// <summary>
    /// Play vibration alert
    /// </summary>
    public static void PlayVibration(int durationMs = 300) {
#if ANDROID
        try {
            var context = global::Android.App.Application.Context;
            var vibrator = (Android.OS.Vibrator?)context.GetSystemService(Android.Content.Context.VibratorService);

            if(vibrator != null && vibrator.HasVibrator) {
                if(Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O) {
                    var effect = Android.OS.VibrationEffect.CreateOneShot(durationMs,Android.OS.VibrationEffect.DefaultAmplitude);
                    vibrator.Vibrate(effect);
                } else {
#pragma warning disable CS0618
                    vibrator.Vibrate(durationMs);
#pragma warning restore CS0618
                }
                System.Diagnostics.Debug.WriteLine($"📳 VIBRATION ({durationMs}ms)");
            }
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"⚠ Vibration error: {ex.Message}");
        }
#endif
    }

    /// <summary>
    /// Play MP3 sound alert
    /// </summary>
    public static async Task PlaySoundAsync(float volume = 0.7f) {
#if ANDROID
        try {
            using var stream = await FileSystem.OpenAppPackageFileAsync("NewSpotArrived.mp3");
            var tempFile = Path.Combine(FileSystem.CacheDirectory,"SpotAlert.mp3");

            using(var fileStream = File.Create(tempFile)) {
                await stream.CopyToAsync(fileStream);
            }

            var player = new MediaPlayer();

            var audioAttributes = new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.Alarm)!
                .SetContentType(AudioContentType.Sonification)!
                .Build();

            player.SetAudioAttributes(audioAttributes);
            player.SetDataSource(tempFile);
            player.Prepare();
            player.SetVolume(volume,volume);
            player.Start();

            System.Diagnostics.Debug.WriteLine($"🔊 MP3 SOUND (vol={volume:P0})");

            player.Completion += (s,e) => {
                try { player.Release(); } catch { }
            };
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"⚠ Sound error: {ex.Message}");
        }
#else
        await Task.CompletedTask;
#endif
    }

    /// <summary>
    /// Play alerts with MP3 sound
    /// </summary>
    public static void PlayAlerts(bool vibrationEnabled,bool soundEnabled,float volume = 0.7f) {
        System.Diagnostics.Debug.WriteLine($"🔔 PlayAlerts: vib={vibrationEnabled}, snd={soundEnabled}, vol={volume:P0}");

        if(vibrationEnabled) {
            PlayVibration(300);
        }

        if(soundEnabled) {
            _ = PlaySoundAsync(volume);
        }
    }

    /// <summary>
    /// Play alerts with TTS announcement
    /// </summary>
    public static void PlayAlertsWithTts(bool vibrationEnabled,bool soundEnabled,float volume,float speed,
                                          string callsign,string band,string mode,DateTime utc) {
        System.Diagnostics.Debug.WriteLine($"🔔 TTS Alert: {callsign}, speed={speed:F1}");

        if(vibrationEnabled) {
            PlayVibration(300);
        }

        if(soundEnabled) {
            _ = TtsService.SpeakSpotAsync(callsign,band,mode,utc,volume,speed);
        }
    }
}