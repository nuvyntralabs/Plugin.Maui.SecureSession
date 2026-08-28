using Microsoft.Extensions.Logging;
using Plugin.Maui.SecureSession;
using Plugin.Maui.SecureSession.Sample.Demo;

namespace Plugin.Maui.SecureSession.Sample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<DemoAuthGateway>();
        builder.Services.AddSingleton<IAuthGateway>(sp => sp.GetRequiredService<DemoAuthGateway>());
        builder.Services.AddSingleton<DemoApiHandler>();
        builder.Services.AddSingleton<MainPage>();

        builder.Services
            .AddHttpClient("shop", client => client.BaseAddress = new Uri("https://api.shop.test"))
            .AddHttpMessageHandler(sp => sp.GetRequiredService<DemoApiHandler>())
            .AddSecureSession();

        builder
            .UseMauiApp<App>()
            .UseSecureSession(options =>
            {
                options.AccessTokenRefreshSkew = TimeSpan.FromSeconds(20);
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.AbsoluteSessionLifetime = TimeSpan.FromHours(12);
                options.LockOnBackground = true;
                options.DeviceName = DeviceInfo.Current.Name;
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
