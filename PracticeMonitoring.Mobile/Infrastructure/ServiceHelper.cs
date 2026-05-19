using Microsoft.Extensions.DependencyInjection;

namespace PracticeMonitoring.Mobile.Infrastructure;

public static class ServiceHelper
{
    public static IServiceProvider Services { get; set; } = null!;

    public static T Get<T>() where T : notnull
    {
        return Services.GetRequiredService<T>();
    }
}
