#if ANDROID
using AndroidTts = Android.Speech.Tts.TextToSpeech;
#endif

namespace DXSpotViewer.Services;

/// <summary>
/// Text-to-Speech service with phonetic alphabet support and speed control
/// </summary>
public static class TtsService {

#if ANDROID
    private static AndroidTts? _tts;
    private static bool _ttsReady;
    private static TaskCompletionSource<bool>? _initTcs;
#endif

    private static readonly Dictionary<char,string> PhoneticAlphabet = new() {
        {'A', "Alpha"}, {'B', "Bravo"}, {'C', "Charlie"}, {'D', "Delta"},
        {'E', "Echo"}, {'F', "Foxtrot"}, {'G', "Golf"}, {'H', "Hotel"},
        {'I', "India"}, {'J', "Juliet"}, {'K', "Kilo"}, {'L', "Lima"},
        {'M', "Mike"}, {'N', "November"}, {'O', "Oscar"}, {'P', "Papa"},
        {'Q', "Quebec"}, {'R', "Romeo"}, {'S', "Sierra"}, {'T', "Tango"},
        {'U', "Uniform"}, {'V', "Victor"}, {'W', "Whiskey"}, {'X', "X-ray"},
        {'Y', "Yankee"}, {'Z', "Zulu"},
        {'0', "Zero"}, {'1', "One"}, {'2', "Two"}, {'3', "Three"},
        {'4', "Four"}, {'5', "Five"}, {'6', "Six"}, {'7', "Seven"},
        {'8', "Eight"}, {'9', "Nine"},
        {'/', "Stroke"}
    };

    /// <summary>
    /// Convert callsign to phonetic alphabet
    /// </summary>
    public static string ToPhonetic(string callsign) {
        if(string.IsNullOrEmpty(callsign)) return "";

        var parts = new List<string>();
        foreach(char c in callsign.ToUpperInvariant()) {
            if(PhoneticAlphabet.TryGetValue(c,out string? phonetic)) {
                parts.Add(phonetic);
            } else {
                parts.Add(c.ToString());
            }
        }
        return string.Join(" ",parts);
    }

    /// <summary>
    /// Format band for speech
    /// </summary>
    public static string FormatBand(string band) {
        if(string.IsNullOrEmpty(band)) return "unknown band";
        band = band.ToLowerInvariant().Trim();
        if(band.EndsWith("m")) {
            return $"{band.TrimEnd('m')} meters";
        }
        return band;
    }

    /// <summary>
    /// Format time for speech (local time, simple format)
    /// </summary>
    public static string FormatTime(DateTime utc) {
        var local = utc.ToLocalTime();
        return $"{local.Hour}:{local.Minute:D2}";
    }

    /// <summary>
    /// Build complete spot announcement
    /// </summary>
    public static string BuildSpotMessage(string callsign,string band,string mode,DateTime utc) {
        var phonetic = ToPhonetic(callsign);
        var bandText = FormatBand(band);
        var timeText = FormatTime(utc);

        return $"Spot: {phonetic}, {bandText}, {mode}, {timeText}";
    }

#if ANDROID
    /// <summary>
    /// Initialize Android TTS engine
    /// </summary>
    private static async Task EnsureTtsInitializedAsync() {
        if(_ttsReady && _tts != null) return;

        _initTcs = new TaskCompletionSource<bool>();
        var context = Android.App.Application.Context;

        _tts = new AndroidTts(context,new TtsInitListener());

        // Wait for init with timeout
        var timeoutTask = Task.Delay(3000);
        var completedTask = await Task.WhenAny(_initTcs.Task,timeoutTask);

        if(completedTask == timeoutTask) {
            System.Diagnostics.Debug.WriteLine("⚠ TTS init timeout");
        }
    }

    private class TtsInitListener:Java.Lang.Object, AndroidTts.IOnInitListener {
        public void OnInit(Android.Speech.Tts.OperationResult status) {
            if(status == Android.Speech.Tts.OperationResult.Success) {
                _ttsReady = true;
                _tts?.SetLanguage(Java.Util.Locale.Us);
                System.Diagnostics.Debug.WriteLine("✅ TTS initialized");
            } else {
                System.Diagnostics.Debug.WriteLine("❌ TTS init failed");
            }
            _initTcs?.TrySetResult(_ttsReady);
        }
    }

    /// <summary>
    /// Speak text with volume and speed control (Android native)
    /// </summary>
    public static async Task SpeakAsync(string text,float volume = 0.7f,float speed = 0.8f) {
        try {
            await EnsureTtsInitializedAsync();

            if(_tts == null || !_ttsReady) {
                System.Diagnostics.Debug.WriteLine("⚠ TTS not available, using MAUI fallback");
                await Microsoft.Maui.Media.TextToSpeech.Default.SpeakAsync(text,
                    new Microsoft.Maui.Media.SpeechOptions { Volume = volume });
                return;
            }

            // ✅ Adjust pitch to compensate for distortion at low speeds
            // Lower speed = slightly higher pitch to maintain clarity
            float pitch = 1.0f;
            if(speed < 0.8f) {
                pitch = 1.0f + (0.8f - speed) * 0.3f; // Gradually increase pitch as speed decreases
            }

            _tts.SetSpeechRate(speed);
            _tts.SetPitch(pitch);

            // Set volume via audio params
            var audioParams = new Android.OS.Bundle();
            audioParams.PutFloat(AndroidTts.Engine.KeyParamVolume,volume);

            _tts.Speak(text,Android.Speech.Tts.QueueMode.Flush,audioParams,"spot_alert");

            System.Diagnostics.Debug.WriteLine($"🗣️ TTS: {text} (speed={speed:F1}, pitch={pitch:F2}, vol={volume:P0})");
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"❌ TTS error: {ex.Message}");
        }
    }
#else
    public static async Task SpeakAsync(string text, float volume = 0.7f, float speed = 0.8f) {
        await Microsoft.Maui.Media.TextToSpeech.Default.SpeakAsync(text, 
            new Microsoft.Maui.Media.SpeechOptions { Volume = volume });
    }
#endif

    /// <summary>
    /// Speak a DX spot announcement
    /// </summary>
    public static async Task SpeakSpotAsync(string callsign,string band,string mode,DateTime utc,
                                             float volume = 0.7f,float speed = 0.8f) {
        string message = BuildSpotMessage(callsign,band,mode,utc);
        await SpeakAsync(message,volume,speed);
    }
}