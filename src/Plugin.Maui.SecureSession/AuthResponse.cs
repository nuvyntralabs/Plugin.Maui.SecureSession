namespace Plugin.Maui.SecureSession;

/// <summary>
/// Result of a login or refresh call against the application's auth server.
/// </summary>
public sealed class AuthResponse
{
    /// <summary>
    /// Gets the tokens to persist.
    /// </summary>
    public required TokenBundle Tokens { get; init; }

    /// <summary>
    /// Gets the account's known device sessions, when the server returns them.
    /// </summary>
    public IReadOnlyList<RemoteSession>? Sessions { get; init; }

    /// <summary>
    /// Gets a value indicating whether the refresh token in <see cref="Tokens"/> replaces the previous one.
    /// </summary>
    public bool RefreshTokenRotated { get; init; } = true;
}
