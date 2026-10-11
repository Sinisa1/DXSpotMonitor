using DXSpotViewer.Services;

namespace DXSpotViewer;

public partial class App:Application {
    private static bool _initialNavigationDone = false;

    public App() {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState) {
        var window = new Window(new AppShell());
        window.Resumed += OnWindowResumed;
        window.Stopped += OnWindowStopped;
        return window;
    }

    protected override async void OnStart() {
        base.OnStart();
        System.Diagnostics.Debug.WriteLine("🚀 App.OnStart()");

        if(_initialNavigationDone) {
            System.Diagnostics.Debug.WriteLine("  Initial navigation already done");
            return;
        }

        await PerformInitialNavigationAsync();
    }

    private async Task PerformInitialNavigationAsync() {
        int retries = 0;
        while(MauiProgram.Services == null && retries < 10) {
            await Task.Delay(100);
            retries++;
        }

        if(MauiProgram.Services == null) {
            System.Diagnostics.Debug.WriteLine("❌ Services not available");
            return;
        }

        try {
            var prefsService = MauiProgram.Services.GetService<PreferencesService>();
            if(prefsService == null) return;

            var userCall = prefsService.GetUserCall();
            var pin = prefsService.GetPin();
            bool hasCredentials = !string.IsNullOrEmpty(userCall) && !string.IsNullOrEmpty(pin);

            _initialNavigationDone = true;

            if(hasCredentials) {
                System.Diagnostics.Debug.WriteLine($"✅ Credentials found → MainPage");
                await Shell.Current.GoToAsync("//MainPage");
            } else {
                System.Diagnostics.Debug.WriteLine("ℹ No credentials → LoginPage");
                await Shell.Current.GoToAsync("//LoginPage");
            }
        } catch(Exception ex) {
            System.Diagnostics.Debug.WriteLine($"❌ Navigation error: {ex.Message}");
        }
    }

    private void OnWindowResumed(object? sender,EventArgs e) {
        System.Diagnostics.Debug.WriteLine("🔄 Window.Resumed");
        // Service handles reconnection, no need to do it here
    }

    private void OnWindowStopped(object? sender,EventArgs e) {
        System.Diagnostics.Debug.WriteLine("😴 Window.Stopped");
        // Service keeps running in background
    }

    protected override void OnResume() {
        base.OnResume();
        System.Diagnostics.Debug.WriteLine("🔄 App.OnResume()");
    }

    protected override void OnSleep() {
        base.OnSleep();
        System.Diagnostics.Debug.WriteLine("😴 App.OnSleep()");
    }
}