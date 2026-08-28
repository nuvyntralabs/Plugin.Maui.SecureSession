using System.Collections.Concurrent;
using Plugin.Maui.SecureSession;

namespace Plugin.Maui.SecureSession.Sample.Demo;

public sealed class DemoAuthGateway : IAuthGateway
{
    readonly ConcurrentDictionary<string, RefreshRecord> _refresh = new();
    readonly ConcurrentDictionary<string, RemoteSession> _sessions = new();

    public bool CanRefresh => true;

    public Task<AuthResponse> LoginAsync(LoginRequest request, DeviceContext device, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.Password, "maui", StringComparison.Ordinal))
        {
            throw new SecureSessionException("Password must be 'maui'.");
        }

        var user = string.IsNullOrWhiteSpace(request.Username) ? "ada" : request.Username.Trim();
        return Task.FromResult(Issue(user, device, generation: 0, device.SessionId));
    }

    public Task<AuthResponse> RefreshAsync(RefreshRequest request, DeviceContext device, CancellationToken cancellationToken)
    {
        if (!_refresh.TryRemove(request.RefreshToken, out var issued))
        {
            var reused = _refresh.Values.Any(item =>
                string.Equals(item.Previous, request.RefreshToken, StringComparison.Ordinal));
            throw new RefreshFailedException(
                reused ? "Refresh token was reused. All sessions should be treated as compromised." : "Refresh token is unknown.",
                reused ? RefreshFailureKind.RefreshTokenReused : RefreshFailureKind.InvalidRefreshToken);
        }

        return Task.FromResult(Issue(issued.UserId, device, issued.Generation + 1, request.SessionId, request.RefreshToken));
    }

    public Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        if (request.Scope == LogoutScope.AllDevices)
        {
            _sessions.Clear();
        }
        else
        {
            _sessions.TryRemove(request.SessionId, out _);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RemoteSession>> GetSessionsAsync(string accessToken, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RemoteSession>>(_sessions.Values.Select(Clone).ToList());

    public Task RevokeSessionAsync(string accessToken, string sessionId, CancellationToken cancellationToken)
    {
        _sessions.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }

    public void AddDemoDevice()
    {
        var id = Guid.NewGuid().ToString("N");
        _sessions[id] = new RemoteSession
        {
            SessionId = id,
            DeviceId = "demo-ipad",
            DeviceName = "Ada's iPad",
            Platform = "iOS",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            LastSeenAt = DateTimeOffset.UtcNow.AddHours(-3)
        };
    }

    AuthResponse Issue(string userId, DeviceContext device, int generation, string sessionId, string? previousRefresh = null)
    {
        var now = DateTimeOffset.UtcNow;
        var refresh = $"refresh.{userId}.{generation}.{Guid.NewGuid():N}";
        _refresh[refresh] = new RefreshRecord(userId, generation, previousRefresh);
        _sessions[sessionId] = new RemoteSession
        {
            SessionId = sessionId,
            DeviceId = device.DeviceId,
            DeviceName = device.DeviceName,
            Platform = device.Platform,
            CreatedAt = now,
            LastSeenAt = now,
            IsCurrent = true
        };

        return new AuthResponse
        {
            RefreshTokenRotated = generation > 0,
            Tokens = new TokenBundle
            {
                AccessToken = $"access.{userId}.{generation}.{Guid.NewGuid():N}",
                RefreshToken = refresh,
                AccessTokenExpiresAt = now.AddSeconds(45),
                RefreshTokenExpiresAt = now.AddDays(14),
                SessionId = sessionId,
                UserId = userId,
                UserName = userId
            }
        };
    }

    static RemoteSession Clone(RemoteSession session) =>
        new()
        {
            SessionId = session.SessionId,
            DeviceId = session.DeviceId,
            DeviceName = session.DeviceName,
            Platform = session.Platform,
            CreatedAt = session.CreatedAt,
            LastSeenAt = session.LastSeenAt,
            IsCurrent = session.IsCurrent
        };

    sealed record RefreshRecord(string UserId, int Generation, string? Previous);
}
