namespace Plugin.Maui.SecureSession;

/// <summary>
/// Mobile authentication session: tokens, refresh, logout, lock, and multi-device control.
/// </summary>
public interface ISecureSession
{
    /// <summary>
    /// Gets the current session lifecycle state.
    /// </summary>
    SessionState State { get; }

    /// <summary>
    /// Gets a value indicating whether the user is signed in and the session is unlocked.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets a value indicating whether a persisted session exists but requires biometric unlock.
    /// </summary>
    bool IsLocked { get; }

    /// <summary>
    /// Gets a public snapshot of the current session, or <c>null</c> when anonymous.
    /// </summary>
    SessionSnapshot? Current { get; }

    /// <summary>
    /// Raised when the session state changes (login, lock, unlock, logout, expiry).
    /// </summary>
    event EventHandler<SessionChangedEventArgs>? SessionChanged;

    /// <summary>
    /// Raised when a persisted session is invalidated and local tokens are cleared.
    /// </summary>
    event EventHandler<SessionExpiredEventArgs>? SessionExpired;

    /// <summary>
    /// Raised after the session is locked.
    /// </summary>
    event EventHandler? Locked;

    /// <summary>
    /// Raised after a successful biometric unlock.
    /// </summary>
    event EventHandler? Unlocked;

    /// <summary>
    /// Signs in through <see cref="IAuthGateway"/> and persists the returned tokens.
    /// </summary>
    Task<SessionSnapshot> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs in with a username and password.
    /// </summary>
    Task<SessionSnapshot> LoginAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts tokens from an external login flow (browser OAuth, OTP, etc.) and persists them.
    /// </summary>
    Task<SessionSnapshot> LoginAsync(TokenBundle tokens, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a valid access token, refreshing it when it is expired or inside the refresh skew.
    /// </summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges the refresh token for a new token bundle. Concurrent callers share one refresh.
    /// </summary>
    Task<TokenBundle> RefreshTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs out locally and, when a gateway is configured, on the server.
    /// </summary>
    Task LogoutAsync(LogoutScope scope = LogoutScope.ThisDevice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reloads the persisted session after process start. Stays locked when biometric unlock is required.
    /// </summary>
    Task RestoreAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Records user activity so idle timeout is measured from now.
    /// </summary>
    Task TouchAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks an authenticated session. <see cref="GetAccessTokenAsync"/> fails until unlock.
    /// </summary>
    Task LockAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Prompts for biometrics and unlocks a locked session.
    /// </summary>
    Task UnlockAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns whether the device can prompt for biometrics.
    /// </summary>
    Task<BiometricAvailability> GetBiometricAvailabilityAsync();

    /// <summary>
    /// Enables biometric unlock after a successful prompt and persists the preference.
    /// </summary>
    Task EnableBiometricUnlockAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables biometric unlock for the current session.
    /// </summary>
    Task DisableBiometricUnlockAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists server-known sessions for this account, including this device.
    /// </summary>
    Task<IReadOnlyList<RemoteSession>> GetSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes another device session. Revoking the current session signs out locally.
    /// </summary>
    Task RevokeSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the app moves to the background. May lock the session.
    /// </summary>
    void NotifyBackground();

    /// <summary>
    /// Called when the app returns to the foreground. May expire an idle or absolute session.
    /// </summary>
    void NotifyForeground();
}
