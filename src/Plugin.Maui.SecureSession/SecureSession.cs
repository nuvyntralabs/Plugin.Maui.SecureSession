namespace Plugin.Maui.SecureSession;

/// <summary>
/// Static entry point for <see cref="ISecureSession"/>.
/// </summary>
public static class SecureSession
{
    static ISecureSession? current;

    /// <summary>
    /// Gets the session registered by <c>UseSecureSession</c> or <see cref="Create(SecureSessionOptions, IAuthGateway)"/>.
    /// </summary>
    public static ISecureSession Current =>
        current ?? throw new InvalidOperationException(
            "SecureSession is not initialized. Call builder.UseSecureSession() or SecureSession.Create().");

    /// <summary>
    /// Creates a session that persists tokens with SecureStoragePlus.
    /// </summary>
    public static ISecureSession Create(SecureSessionOptions? options = null, IAuthGateway? gateway = null)
    {
        options ??= new SecureSessionOptions();
        gateway ??= new DelegateAuthGateway(options);
        var session = Create(
            options,
            gateway,
            new SecureStoragePlusSessionStore(global::Plugin.Maui.SecureStoragePlus.SecureStoragePlus.Default),
            PlatformBiometricGate.Create(),
            SystemClock.Instance,
            new MauiDeviceIdentity());
        SetCurrent(session);
        return session;
    }

    internal static SecureSessionImplementation Create(
        SecureSessionOptions options,
        IAuthGateway gateway,
        ISecureSessionStore store,
        IBiometricGate biometrics,
        IClock clock,
        IDeviceIdentity device)
    {
        var session = new SecureSessionImplementation(options, gateway, store, biometrics, clock, device);
        return session;
    }

    internal static void SetCurrent(ISecureSession? session) => current = session;
}
