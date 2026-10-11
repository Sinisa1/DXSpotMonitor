namespace DXSpotViewer.Services;

/// <summary>
/// Service to persist user credentials using MAUI Preferences API
/// </summary>
public class PreferencesService {
    private const string KEY_USER_CALL = "user_callsign";
    private const string KEY_PIN = "user_pin";
    private const string KEY_FIRST_RUN = "first_run";
    private const string KEY_ENABLE_VIBRATION = "enable_vibration";
    private const string KEY_ENABLE_SOUND = "enable_sound";
    private const string KEY_NOTIFY_STN = "notify_stn";
    private const string KEY_ALERT_VOLUME = "alert_volume";
    private const string KEY_USE_TTS = "use_tts";
    private const string KEY_SPEECH_SPEED = "speech_speed";

    /// <summary>
    /// Get speech speed (0.5 to 1.5, default 0.8)
    /// </summary>
    public float GetSpeechSpeed() {
        return Preferences.Default.Get(KEY_SPEECH_SPEED,0.8f);
    }

    /// <summary>
    /// Set speech speed
    /// </summary>
    public void SetSpeechSpeed(float speed) {
        speed = Math.Clamp(speed,0.5f,1.5f);
        Preferences.Default.Set(KEY_SPEECH_SPEED,speed);
    }
    /// <summary>
    /// Get whether to use Text-to-Speech instead of MP3
    /// </summary>
    public bool GetUseTts() {
        return Preferences.Default.Get(KEY_USE_TTS,false);
    }

    /// <summary>
    /// Set whether to use Text-to-Speech instead of MP3
    /// </summary>
    public void SetUseTts(bool enabled) {
        Preferences.Default.Set(KEY_USE_TTS,enabled);
    }
    /// <summary>
    /// Get alert volume (0.0 to 1.0, default 0.7)
    /// </summary>
    public float GetAlertVolume() {
        return Preferences.Default.Get(KEY_ALERT_VOLUME,0.7f);
    }

    /// <summary>
    /// Set alert volume (0.0 to 1.0)
    /// </summary>
    public void SetAlertVolume(float volume) {
        volume = Math.Clamp(volume,0.0f,1.0f);
        Preferences.Default.Set(KEY_ALERT_VOLUME,volume);
    }
    public bool GetVibrationEnabled() {
        return Preferences.Default.Get(KEY_ENABLE_VIBRATION,true);
    }

    public void SetVibrationEnabled(bool enabled) {
        Preferences.Default.Set(KEY_ENABLE_VIBRATION,enabled);
    }

    public bool GetSoundEnabled() {
        return Preferences.Default.Get(KEY_ENABLE_SOUND,true);
    }
    public bool GetNotifyForSTN() {
        return Preferences.Default.Get(KEY_NOTIFY_STN,true);
    }
    public void SetNotifyForSTN(bool enabled) {
        Preferences.Default.Set(KEY_NOTIFY_STN,enabled);
    }
    public void SetSoundEnabled(bool enabled) {
        Preferences.Default.Set(KEY_ENABLE_SOUND,enabled);
    }

    public bool IsFirstRun() {
        return Preferences.Default.Get(KEY_FIRST_RUN,true);
    }

    public void SetFirstRunComplete() {
        Preferences.Default.Set(KEY_FIRST_RUN,false);
    }

    public void SetUserCall(string userCall) {
        Preferences.Default.Set(KEY_USER_CALL,userCall.ToUpper());
    }

    public string GetUserCall() {
        return Preferences.Default.Get(KEY_USER_CALL,string.Empty);
    }

    public void SetPin(string pin) {
        Preferences.Default.Set(KEY_PIN,pin);
    }

    public string GetPin() {
        return Preferences.Default.Get(KEY_PIN,string.Empty);
    }

    public bool HasSavedCredentials() {
        var userCall = GetUserCall();
        var pin = GetPin();
        return !string.IsNullOrEmpty(userCall) && !string.IsNullOrEmpty(pin);
    }

    /// <summary>
    /// Clear all saved credentials (for logout)
    /// </summary>
    public void ClearCredentials() {
        Preferences.Default.Remove(KEY_USER_CALL);
        Preferences.Default.Remove(KEY_PIN);
        System.Diagnostics.Debug.WriteLine("Credentials cleared");
    }
}