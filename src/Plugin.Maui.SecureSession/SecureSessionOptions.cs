namespace Plugin.Maui.SecureSession;

/// <summary>
/// Configuration for <see cref="ISecureSession"/>.
/// </summary>
public sealed class SecureSessionOptions
{
    /// <summary>
    /// Gets or sets how early an access token is treated as expired so refresh happens before APIs fail.
    /// Default is 60 seconds.
    /// </summary>
    public TimeSpan AccessTokenRefreshSkew { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the maximum lifetime of a login, measured from issue time.
    /// When <c>null</c>, only token and idle rules apply.
    /// </summary>
    public TimeSpan? AbsoluteSessionLifetime { get; set; }

    /// <summary>
    /// Gets or sets how long the session may sit unused before it expires.
    /// </summary>
    public TimeSpan? IdleTimeout { get; set; }

    /// <summary>
    /// Gets or sets how long a just-rotated refresh token is still accepted locally
    /// so in-flight callers that missed the rotation do not force a second server call.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan RefreshReuseGrace { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a value indicating whether a new refresh token from the server replaces the previous one.
    /// Default is <c>true</c>.
    /// </summary>
    public bool RotateRefreshTokens { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether a restored session starts locked and requires biometrics.
    /// </summary>
    public bool RequireBiometricUnlock { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the session locks when the app backgrounds.
    /// Default is <c>true</c> when biometric unlock is enabled on the session.
    /// </summary>
    public bool LockOnBackground { get; set; } = true;

    /// <summary>
    /// Gets or sets the reason string shown on the biometric prompt.
    /// </summary>
    public string BiometricPromptReason { get; set; } = "Unlock your session";

    /// <summary>
    /// Gets or sets the HTTP scheme written on outgoing requests. Default is <c>Bearer</c>.
    /// </summary>
    public string AuthenticationScheme { get; set; } = "Bearer";

    /// <summary>
    /// Gets the status codes that trigger a refresh and retry. Default is HTTP 401.
    /// </summary>
    public IList<HttpStatusCode> UnauthorizedStatusCodes { get; } = new List<HttpStatusCode>
    {
        HttpStatusCode.Unauthorized
    };

    /// <summary>
    /// Gets or sets an override for the device display name sent to the auth server.
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Gets optional callbacks.
    /// </summary>
    public SecureSessionEvents Events { get; } = new();

    /// <summary>
    /// Gets or sets a login delegate used when no <see cref="IAuthGateway"/> is registered.
    /// </summary>
    public Func<LoginRequest, DeviceContext, CancellationToken, Task<AuthResponse>>? LoginAsync { get; set; }

    /// <summary>
    /// Gets or sets a refresh delegate used when no <see cref="IAuthGateway"/> is registered.
    /// </summary>
    public Func<RefreshRequest, DeviceContext, CancellationToken, Task<AuthResponse>>? RefreshAsync { get; set; }

    /// <summary>
    /// Gets or sets a logout delegate used when no <see cref="IAuthGateway"/> is registered.
    /// </summary>
    public Func<LogoutRequest, CancellationToken, Task>? LogoutAsync { get; set; }

    /// <summary>
    /// Gets or sets a session-list delegate used when no <see cref="IAuthGateway"/> is registered.
    /// </summary>
    public Func<string, CancellationToken, Task<IReadOnlyList<RemoteSession>>>? GetSessionsAsync { get; set; }

    /// <summary>
    /// Gets or sets a revoke delegate used when no <see cref="IAuthGateway"/> is registered.
    /// </summary>
    public Func<string, string, CancellationToken, Task>? RevokeSessionAsync { get; set; }
}
