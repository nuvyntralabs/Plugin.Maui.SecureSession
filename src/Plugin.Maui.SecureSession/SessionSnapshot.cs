namespace Plugin.Maui.SecureSession;

/// <summary>
/// Public, non-secret view of the current session.
/// </summary>
public sealed class SessionSnapshot
{
    /// <summary>
    /// Gets the current lifecycle state.
    /// </summary>
    public required SessionState State { get; init; }

    /// <summary>
    /// Gets this device's session id.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Gets this installation's device id.
    /// </summary>
    public required string DeviceId { get; init; }

    /// <summary>
    /// Gets the device display name stored with the session.
    /// </summary>
    public string? DeviceName { get; init; }

    /// <summary>
    /// Gets the platform stored with the session.
    /// </summary>
    public string? Platform { get; init; }

    /// <summary>
    /// Gets the signed-in user id.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets the signed-in user display name.
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// Gets when the session was established.
    /// </summary>
    public DateTimeOffset IssuedAt { get; init; }

    /// <summary>
    /// Gets the last recorded activity time.
    /// </summary>
    public DateTimeOffset LastActivityAt { get; init; }

    /// <summary>
    /// Gets when the access token expires.
    /// </summary>
    public DateTimeOffset? AccessTokenExpiresAt { get; init; }

    /// <summary>
    /// Gets when the refresh token expires.
    /// </summary>
    public DateTimeOffset? RefreshTokenExpiresAt { get; init; }

    /// <summary>
    /// Gets the absolute session end, when configured.
    /// </summary>
    public DateTimeOffset? SessionExpiresAt { get; init; }

    /// <summary>
    /// Gets a value indicating whether a refresh token is stored.
    /// </summary>
    public bool HasRefreshToken { get; init; }

    /// <summary>
    /// Gets a value indicating whether biometric unlock is required after lock or process death.
    /// </summary>
    public bool BiometricEnabled { get; init; }

    /// <summary>
    /// Gets how many times the refresh token has rotated.
    /// </summary>
    public int RefreshGeneration { get; init; }

    /// <summary>
    /// Gets optional non-secret claims.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Claims { get; init; }
}
