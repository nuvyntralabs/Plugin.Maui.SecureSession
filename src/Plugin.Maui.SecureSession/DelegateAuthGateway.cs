namespace Plugin.Maui.SecureSession;

/// <summary>
/// <see cref="IAuthGateway"/> backed by the delegates on <see cref="SecureSessionOptions"/>.
/// </summary>
public sealed class DelegateAuthGateway : IAuthGateway
{
    readonly SecureSessionOptions _options;

    /// <summary>
    /// Creates a gateway that forwards to <paramref name="options"/>.
    /// </summary>
    public DelegateAuthGateway(SecureSessionOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public bool CanRefresh => _options.RefreshAsync is not null;

    /// <inheritdoc />
    public Task<AuthResponse> LoginAsync(LoginRequest request, DeviceContext device, CancellationToken cancellationToken)
    {
        if (_options.LoginAsync is null)
        {
            throw new SecureSessionException(
                "No login handler is configured. Register IAuthGateway or set SecureSessionOptions.LoginAsync.");
        }

        return _options.LoginAsync(request, device, cancellationToken);
    }

    /// <inheritdoc />
    public Task<AuthResponse> RefreshAsync(RefreshRequest request, DeviceContext device, CancellationToken cancellationToken)
    {
        if (_options.RefreshAsync is null)
        {
            throw new RefreshFailedException(
                "No refresh handler is configured. Register IAuthGateway or set SecureSessionOptions.RefreshAsync.",
                RefreshFailureKind.InvalidRefreshToken);
        }

        return _options.RefreshAsync(request, device, cancellationToken);
    }

    /// <inheritdoc />
    public Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        return _options.LogoutAsync is null
            ? Task.CompletedTask
            : _options.LogoutAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RemoteSession>> GetSessionsAsync(string accessToken, CancellationToken cancellationToken)
    {
        return _options.GetSessionsAsync is null
            ? Task.FromResult<IReadOnlyList<RemoteSession>>(Array.Empty<RemoteSession>())
            : _options.GetSessionsAsync(accessToken, cancellationToken);
    }

    /// <inheritdoc />
    public Task RevokeSessionAsync(string accessToken, string sessionId, CancellationToken cancellationToken)
    {
        return _options.RevokeSessionAsync is null
            ? Task.CompletedTask
            : _options.RevokeSessionAsync(accessToken, sessionId, cancellationToken);
    }
}
