namespace Plugin.Maui.SecureSession;

/// <summary>
/// Application-owned bridge to the authorization server.
/// Register an implementation in DI, or set the delegates on <see cref="SecureSessionOptions"/>.
/// </summary>
public interface IAuthGateway
{
    /// <summary>
    /// Gets a value indicating whether this gateway can exchange a refresh token.
    /// </summary>
    bool CanRefresh { get; }

    /// <summary>
    /// Authenticates the user and returns tokens for this device.
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request, DeviceContext device, CancellationToken cancellationToken);

    /// <summary>
    /// Exchanges a refresh token. Implementations should treat reuse of a rotated token as a security event
    /// and throw <see cref="RefreshFailedException"/> with <see cref="RefreshFailureKind.RefreshTokenReused"/>.
    /// </summary>
    Task<AuthResponse> RefreshAsync(RefreshRequest request, DeviceContext device, CancellationToken cancellationToken);

    /// <summary>
    /// Ends one or every server session for the account.
    /// </summary>
    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the account's known device sessions.
    /// </summary>
    Task<IReadOnlyList<RemoteSession>> GetSessionsAsync(string accessToken, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes a single server session.
    /// </summary>
    Task RevokeSessionAsync(string accessToken, string sessionId, CancellationToken cancellationToken);
}
