namespace Plugin.Maui.SecureSession;

/// <summary>
/// Access and refresh tokens plus the metadata the session needs to persist them.
/// </summary>
public sealed class TokenBundle
{
    /// <summary>
    /// Gets the access token presented to APIs.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Gets the refresh token used to rotate the access token.
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Gets when the access token expires. When omitted, a JWT <c>exp</c> claim is used if present.
    /// </summary>
    public DateTimeOffset? AccessTokenExpiresAt { get; init; }

    /// <summary>
    /// Gets when the refresh token expires.
    /// </summary>
    public DateTimeOffset? RefreshTokenExpiresAt { get; init; }

    /// <summary>
    /// Gets the absolute end of this login session, independent of token lifetimes.
    /// </summary>
    public DateTimeOffset? SessionExpiresAt { get; init; }

    /// <summary>
    /// Gets the server-assigned session id for this device, when the auth server issues one.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Gets the signed-in user id.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets a display name for the signed-in user.
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// Gets optional non-secret claims stored with the session.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Claims { get; init; }
}
