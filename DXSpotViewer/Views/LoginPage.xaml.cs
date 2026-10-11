using DXSpotViewer.Services;

namespace DXSpotViewer.Views;

public partial class LoginPage:ContentPage {
    private readonly MqttService? _mqttService;
    private readonly PreferencesService? _preferencesService;

    public LoginPage() {
        InitializeComponent();

        try {
            _mqttService = MauiProgram.Services?.GetService<MqttService>();
            _preferencesService = MauiProgram.Services?.GetService<PreferencesService>();

            if(_preferencesService?.HasSavedCredentials() == true) {
                UserCallEntry.Text = _preferencesService.GetUserCall();
                PinEntry.Text = _preferencesService.GetPin();
            }
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"LoginPage init error: {ex}");
        }
    }

    private async void OnLoginButtonClicked(object sender,EventArgs e) {
        try {
            ErrorLabel.IsVisible = false;

            var userCall = UserCallEntry.Text?.Trim().ToUpper() ?? "";
            var pin = PinEntry.Text?.Trim() ?? "";

            if(string.IsNullOrEmpty(userCall)) {
                ShowError("Please enter your callsign");
                return;
            }

            if(string.IsNullOrEmpty(pin) || pin.Length != 4) {
                ShowError("Please enter a 4-digit PIN");
                return;
            }

            if(_preferencesService == null || _mqttService == null) {
                ShowError("Services not available");
                return;
            }

            _preferencesService.SetUserCall(userCall);
            _preferencesService.SetPin(pin);

            try {
                await _mqttService.ConnectAsync(userCall);

                // ✅ Start background service AFTER successful connection
#if ANDROID
                StartBackgroundService();
#endif

                await Shell.Current.GoToAsync("//MainPage");
            } catch(Exception ex) {
                ShowError($"Connection failed: {ex.Message}");
            }
        } catch(Exception ex) {
            ShowError($"Login error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Login error: {ex}");
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
            System.Diagnostics.Debug.WriteLine("✅ Background service started from LoginPage");
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"⚠ Failed to start background service: {ex.Message}");
        }
    }
#endif

    private void ShowError(string message) {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}