namespace Plugin.Maui.SecureSession;

/// <summary>
/// Dependency injection helpers for SecureSession.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers session services. Prefer <see cref="MauiAppBuilderExtensions.UseSecureSession"/>.
    /// </summary>
    public static IServiceCollection AddSecureSession(this IServiceCollection services, Action<SecureSessionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SecureSessionOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);
        services.TryAddSingleton<IAuthGateway>(sp => new DelegateAuthGateway(sp.GetRequiredService<SecureSessionOptions>()));
        services.TryAddSingleton<ISecureSessionStore>(sp =>
            new SecureStoragePlusSessionStore(sp.GetService<ISecureStoragePlus>() ?? global::Plugin.Maui.SecureStoragePlus.SecureStoragePlus.Default));
        services.TryAddSingleton<IBiometricGate>(_ => PlatformBiometricGate.Create());
        services.TryAddSingleton<IClock>(_ => SystemClock.Instance);
        services.TryAddSingleton<IDeviceIdentity>(_ => new MauiDeviceIdentity());
        services.TryAddSingleton<ISecureSession>(sp =>
        {
            var session = SecureSession.Create(
                sp.GetRequiredService<SecureSessionOptions>(),
                sp.GetRequiredService<IAuthGateway>(),
                sp.GetRequiredService<ISecureSessionStore>(),
                sp.GetRequiredService<IBiometricGate>(),
                sp.GetRequiredService<IClock>(),
                sp.GetRequiredService<IDeviceIdentity>());
            SecureSession.SetCurrent(session);
            return session;
        });

        return services;
    }
}
