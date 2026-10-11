using DXSpotViewer.Services;

namespace DXSpotViewer.Views;

public partial class SettingsPage:ContentPage {
    private MqttService? _mqttService;
    private PreferencesService? _preferencesService;

    public SettingsPage() {
        InitializeComponent();
    }

    protected override void OnAppearing() {
        base.OnAppearing();

        try {
            _mqttService = MauiProgram.Services?.GetService<MqttService>();
            _preferencesService = MauiProgram.Services?.GetService<PreferencesService>();

            LoadSettings();
            UpdateConnectionStatus();

            if(_mqttService != null) {
                _mqttService.ConnectionChanged -= OnConnectionChanged;
                _mqttService.ConnectionChanged += OnConnectionChanged;
            }
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"❌ SettingsPage error: {ex}");
        }
    }

    protected override void OnDisappearing() {
        base.OnDisappearing();
        if(_mqttService != null) {
            _mqttService.ConnectionChanged -= OnConnectionChanged;
        }
    }

    private void LoadSettings() {
        try {
            var userCall = _preferencesService?.GetUserCall() ?? "";
            CurrentUserLabel.Text = string.IsNullOrEmpty(userCall) ? "Not logged in" : userCall;

            bool vibration = _preferencesService?.GetVibrationEnabled() ?? true;
            bool sound = _preferencesService?.GetSoundEnabled() ?? true;
            bool stn = _preferencesService?.GetNotifyForSTN() ?? true;
            bool useTts = _preferencesService?.GetUseTts() ?? false;

            VibrationSwitch.IsToggled = vibration;
            SoundSwitch.IsToggled = sound;
            TypeSTNSwitch.IsToggled = stn;
            TtsSwitch.IsToggled = useTts;

            // Set initial frame colors based on toggle states
            UpdateFrameColor(VibrationFrame,vibration,"#e8f5e9","#2e7d32","#ffebee","#c62828");
            UpdateFrameColor(SoundFrame,sound,"#e8f5e9","#2e7d32","#ffebee","#c62828");
            UpdateFrameColor(StnFrame,stn,"#fff8e1","#f57c00","#fbe9e7","#e65100");
            UpdateFrameColor(TtsFrame,useTts,"#e1f5fe","#0277bd","#e3f2fd","#1565c0");

            float volume = _preferencesService?.GetAlertVolume() ?? 0.7f;
            VolumeSlider.Value = volume;
            VolumeLabel.Text = $"{volume:P0}";

            float speed = _preferencesService?.GetSpeechSpeed() ?? 0.8f;
            SpeedSlider.Value = speed;
            SpeedLabel.Text = $"{speed:F1}x";

            VersionLabel.Text = "DX Spot Viewer v1.0";
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"❌ LoadSettings error: {ex}");
        }
    }

    /// <summary>
    /// Helper method to update frame and label colors
    /// </summary>
    private void UpdateFrameColor(Frame frame,bool isOn,
                                   string onBgColor,string onTextColor,
                                   string offBgColor,string offTextColor) {
        try {
            frame.BackgroundColor = isOn ? Color.FromArgb(onBgColor) : Color.FromArgb(offBgColor);

            // Get the Grid inside the Frame, then the Label (first child)
            if(frame.Content is Grid grid && grid.Children.Count > 0) {
                if(grid.Children[0] is Label label) {
                    label.TextColor = isOn ? Color.FromArgb(onTextColor) : Color.FromArgb(offTextColor);
                }
            }
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"❌ UpdateFrameColor error: {ex.Message}");
        }
    }

    private void OnVolumeChanged(object sender,ValueChangedEventArgs e) {
        float volume = (float)e.NewValue;
        VolumeLabel.Text = $"{volume:P0}";
        _preferencesService?.SetAlertVolume(volume);
    }

    private void OnSpeedChanged(object sender,ValueChangedEventArgs e) {
        float speed = (float)e.NewValue;
        SpeedLabel.Text = $"{speed:F1}x";
        _preferencesService?.SetSpeechSpeed(speed);
    }

    private void OnVibrationToggled(object sender,ToggledEventArgs e) {
        _preferencesService?.SetVibrationEnabled(e.Value);
        UpdateFrameColor(VibrationFrame,e.Value,"#e8f5e9","#2e7d32","#ffebee","#c62828");
    }

    private void OnSoundToggled(object sender,ToggledEventArgs e) {
        _preferencesService?.SetSoundEnabled(e.Value);
        UpdateFrameColor(SoundFrame,e.Value,"#e8f5e9","#2e7d32","#ffebee","#c62828");
    }

    private void OnTypeSTNToggled(object sender,ToggledEventArgs e) {
        _preferencesService?.SetNotifyForSTN(e.Value);
        UpdateFrameColor(StnFrame,e.Value,"#fff8e1","#f57c00","#fbe9e7","#e65100");
    }

    private void OnTtsToggled(object sender,ToggledEventArgs e) {
        _preferencesService?.SetUseTts(e.Value);
        UpdateFrameColor(TtsFrame,e.Value,"#e1f5fe","#0277bd","#e3f2fd","#1565c0");
        System.Diagnostics.Debug.WriteLine($"⚙️ TTS: {e.Value}");
    }

    private void UpdateConnectionStatus() {
        try {
            if(_mqttService?.IsConnected == true) {
                ConnectionStatusLabel.Text = "Connected";
                ConnectionIndicator.Color = Colors.LimeGreen;
                ConnectionFrame.BackgroundColor = Colors.LimeGreen;
                TopicLabel.Text = $"Topic: hamradio/va3tnm/viewer/{_mqttService.UserCall}";
            } else {
                ConnectionStatusLabel.Text = "Disconnected";
                ConnectionIndicator.Color = Colors.Red;
                ConnectionFrame.BackgroundColor = Colors.Red;
                TopicLabel.Text = "";
            }
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"❌ UpdateConnectionStatus error: {ex}");
        }
    }

    private void OnConnectionChanged(object? sender,bool isConnected) {
        MainThread.BeginInvokeOnMainThread(UpdateConnectionStatus);
    }

    private async void OnChangeCredentialsClicked(object sender,EventArgs e) {
        var result = await DisplayAlert("Change Credentials",
            "You will be logged out and can enter new credentials.","Continue","Cancel");
        if(result) await LogoutAsync();
    }

    private async void OnLogoutClicked(object sender,EventArgs e) {
        await LogoutAsync();
    }

    private async Task LogoutAsync() {
        try {
            _preferencesService?.ClearCredentials();
            if(_mqttService != null) await _mqttService.DisconnectAsync();
            await Shell.Current.GoToAsync("//LoginPage");
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"❌ Logout error: {ex}");
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }

    private async void OnRequestBatteryOptimizationClicked(object sender,EventArgs e) {
#if ANDROID
        try {
            var context = Android.App.Application.Context;
            var pm = context.GetSystemService(Android.Content.Context.PowerService) as Android.OS.PowerManager;

            if(pm != null && !pm.IsIgnoringBatteryOptimizations(context.PackageName)) {
                var intent = new Android.Content.Intent(Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
                intent.SetData(Android.Net.Uri.Parse($"package:{context.PackageName}"));
                intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                context.StartActivity(intent);
            } else {
                await DisplayAlert("Info","Battery optimization already disabled.","OK");
            }
        } catch(Exception ex) {
            await DisplayAlert("Error",$"Failed: {ex.Message}","OK");
        }
#else
        await DisplayAlert("Info", "Only available on Android.", "OK");
#endif
    }

    private async void OnTestAlertsClicked(object sender,EventArgs e) {
        bool vibEnabled = _preferencesService?.GetVibrationEnabled() ?? true;
        bool sndEnabled = _preferencesService?.GetSoundEnabled() ?? true;
        bool useTts = _preferencesService?.GetUseTts() ?? false;
        float volume = _preferencesService?.GetAlertVolume() ?? 0.7f;
        float speed = _preferencesService?.GetSpeechSpeed() ?? 0.8f;

        if(useTts) {
            AlertService.PlayAlertsWithTts(vibEnabled,sndEnabled,volume,speed,
                "VA3TNM","20m","CW",DateTime.UtcNow);
        } else {
            AlertService.PlayAlerts(vibEnabled,sndEnabled,volume);
        }

        await DisplayAlert("🔊 Test",
            $"Mode: {(useTts ? "Voice (TTS)" : "MP3 Sound")}\n" +
            $"Volume: {volume:P0}\n" +
            $"Speed: {speed:F1}x","OK");
    }
}