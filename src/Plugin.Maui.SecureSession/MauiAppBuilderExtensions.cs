using Microsoft.Maui.LifecycleEvents;

namespace Plugin.Maui.SecureSession;

/// <summary>
/// MAUI host registration for SecureSession.
/// </summary>
public static class MauiAppBuilderExtensions
{
    /// <summary>
    /// Registers <see cref="ISecureSession"/>, SecureStoragePlus persistence, and lock/unlock lifecycle hooks.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.UseSecureSession(options =>
    /// {
    ///     options.IdleTimeout = TimeSpan.FromMinutes(15);
    ///     options.RequireBiometricUnlock = true;
    /// });
    /// </code>
    /// </example>
    public static MauiAppBuilder UseSecureSession(this MauiAppBuilder builder, Action<SecureSessionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSecureStoragePlus();
        builder.Services.AddSecureSession(configure);
        builder.Services.AddTransient<IMauiInitializeService, SecureSessionInitializer>();

        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android =>
            {
                android.OnPause(_ => SecureSession.Current.NotifyBackground());
                android.OnResume(_ => SecureSession.Current.NotifyForeground());
            });
#elif IOS
            events.AddiOS(ios =>
            {
                ios.DidEnterBackground(_ => SecureSession.Current.NotifyBackground());
                ios.OnActivated(_ => SecureSession.Current.NotifyForeground());
            });
#endif
        });

        return builder;
    }
}
