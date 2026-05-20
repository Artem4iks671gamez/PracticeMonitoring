using Microsoft.Extensions.Logging;
using PracticeMonitoring.Mobile.Infrastructure;
using PracticeMonitoring.Mobile.Services;

namespace PracticeMonitoring.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<AppSession>();
        builder.Services.AddSingleton<ApiClient>();
        builder.Services.AddSingleton<FileStorageService>();
        builder.Services.AddSingleton<PushRegistrationService>();

        var app = builder.Build();
        ServiceHelper.Services = app.Services;
        return app;
    }
}
