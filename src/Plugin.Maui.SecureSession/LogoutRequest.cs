namespace Plugin.Maui.SecureSession;

/// <summary>
/// Payload sent to the auth server on logout or revoke.
/// </summary>
public sealed class LogoutRequest
{
    /// <summary>
    /// Gets the current access token, when still present.
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// Gets the current refresh token, when still present.
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Gets this device's session id.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Gets this installation's stable device id.
    /// </summary>
    public required string DeviceId { get; init; }

    /// <summary>
    /// Gets whether to end only this device or every session.
    /// </summary>
    public LogoutScope Scope { get; init; } = LogoutScope.ThisDevice;
}
