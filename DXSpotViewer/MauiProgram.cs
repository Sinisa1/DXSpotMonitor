using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using DXSpotViewer.Services;
using DXSpotViewer.Views;

namespace DXSpotViewer;

public static class MauiProgram {
    // ✅ Add this property to access services globally
    public static IServiceProvider Services { get; private set; } = null!;

    public static MauiApp CreateMauiApp() {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts => {
                fonts.AddFont("OpenSans-Regular.ttf","OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf","OpenSansSemibold");
            });

        // ✅ Register services
        builder.Services.AddSingleton<MqttService>();
        builder.Services.AddSingleton<PreferencesService>();

        // ✅ Register pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        
        // ✅ Store services for global access
        Services = app.Services;
        
        return app;
    }
}