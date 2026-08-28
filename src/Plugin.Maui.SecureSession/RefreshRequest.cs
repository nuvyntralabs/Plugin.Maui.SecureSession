namespace Plugin.Maui.SecureSession;

/// <summary>
/// Payload sent to the auth server when exchanging a refresh token.
/// </summary>
public sealed class RefreshRequest
{
    /// <summary>
    /// Gets the refresh token to present.
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// Gets the access token that just failed or expired, when still available.
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// Gets this device's session id.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Gets this installation's stable device id.
    /// </summary>
    public required string DeviceId { get; init; }

    /// <summary>
    /// Gets how many times this session has already rotated the refresh token.
    /// </summary>
    public int RefreshGeneration { get; init; }
}
