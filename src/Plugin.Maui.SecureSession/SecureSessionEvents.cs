namespace Plugin.Maui.SecureSession;

/// <summary>
/// Optional callbacks configured on <see cref="SecureSessionOptions"/>.
/// </summary>
public sealed class SecureSessionEvents
{
    /// <summary>
    /// Called after a successful login.
    /// </summary>
    public Action<SessionSnapshot>? OnLoggedIn { get; set; }

    /// <summary>
    /// Called after tokens are refreshed.
    /// </summary>
    public Action<TokenBundle>? OnTokenRefreshed { get; set; }

    /// <summary>
    /// Called after logout.
    /// </summary>
    public Action<LogoutScope>? OnLoggedOut { get; set; }

    /// <summary>
    /// Called after the session is invalidated.
    /// </summary>
    public Action<SessionExpiryReason>? OnExpired { get; set; }

    /// <summary>
    /// Called after the session is locked.
    /// </summary>
    public Action? OnLocked { get; set; }

    /// <summary>
    /// Called after a successful unlock.
    /// </summary>
    public Action? OnUnlocked { get; set; }

    /// <summary>
    /// Called after a remote session is revoked.
    /// </summary>
    public Action<string>? OnSessionRevoked { get; set; }
}
