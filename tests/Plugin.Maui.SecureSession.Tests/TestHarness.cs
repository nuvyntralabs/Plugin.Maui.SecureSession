using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace Plugin.Maui.SecureSession.Tests;

sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 28, 14, 0, 0, TimeSpan.Zero);

    public void Advance(TimeSpan duration) => UtcNow += duration;
}

sealed class FakeDeviceIdentity : IDeviceIdentity
{
    public string DeviceName { get; set; } = "Pixel Test";

    public string Platform { get; set; } = "Android";

    public string GetDeviceName() => DeviceName;

    public string GetPlatform() => Platform;
}

sealed class FakeBiometricGate : IBiometricGate
{
    public BiometricAvailability Availability { get; set; } = BiometricAvailability.Available;

    public bool Succeed { get; set; } = true;

    public int AuthenticateCalls { get; private set; }

    public Task<BiometricAvailability> GetAvailabilityAsync() => Task.FromResult(Availability);

    public Task<bool> AuthenticateAsync(string reason, CancellationToken cancellationToken)
    {
        AuthenticateCalls++;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Succeed);
    }
}

sealed class FakeAuthGateway : IAuthGateway
{
    readonly ConcurrentDictionary<string, IssuedRefresh> _refresh = new();
    readonly ConcurrentDictionary<string, RemoteSession> _sessions = new();

    public bool CanRefresh { get; set; } = true;

    public TimeSpan AccessLifetime { get; set; } = TimeSpan.FromMinutes(15);

    public TimeSpan RefreshLifetime { get; set; } = TimeSpan.FromDays(14);

    public TimeSpan? RefreshDelay { get; set; }

    public int LoginCalls { get; private set; }

    public int RefreshCalls { get; private set; }

    public int LogoutCalls { get; private set; }

    public LogoutScope? LastLogoutScope { get; private set; }

    public string? LastPassword { get; private set; }

    public IClock? Clock { get; set; }

    public Func<RefreshRequest, AuthResponse>? RefreshOverride { get; set; }

    public Task<AuthResponse> LoginAsync(LoginRequest request, DeviceContext device, CancellationToken cancellationToken)
    {
        LoginCalls++;
        LastPassword = request.Password;
        if (request.Password == "wrong")
        {
            throw new SecureSessionException("Invalid credentials.");
        }

        return Task.FromResult(Issue(request.Username ?? "user", device, generation: 0));
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, DeviceContext device, CancellationToken cancellationToken)
    {
        RefreshCalls++;
        if (RefreshDelay is { } delay)
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        if (RefreshOverride is not null)
        {
            return RefreshOverride(request);
        }

        if (!_refresh.TryGetValue(request.RefreshToken, out var issued))
        {
            var reused = _refresh.Values.Any(value =>
                string.Equals(value.Previous, request.RefreshToken, StringComparison.Ordinal));
            throw new RefreshFailedException(
                reused ? "Refresh token reused." : "Unknown refresh token.",
                reused ? RefreshFailureKind.RefreshTokenReused : RefreshFailureKind.InvalidRefreshToken);
        }

        _refresh.TryRemove(request.RefreshToken, out _);
        return Issue(issued.UserId, device, issued.Generation + 1, request.SessionId, issued.Previous);
    }

    public Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        LogoutCalls++;
        LastLogoutScope = request.Scope;
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
        Task.FromResult<IReadOnlyList<RemoteSession>>(_sessions.Values.ToList());

    public Task RevokeSessionAsync(string accessToken, string sessionId, CancellationToken cancellationToken)
    {
        _sessions.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }

    public void AddDevice(string sessionId, string deviceId, string name, string platform)
    {
        _sessions[sessionId] = new RemoteSession
        {
            SessionId = sessionId,
            DeviceId = deviceId,
            DeviceName = name,
            Platform = platform,
            CreatedAt = DateTimeOffset.UtcNow,
            IsCurrent = false
        };
    }

    AuthResponse Issue(string userId, DeviceContext device, int generation, string? sessionId = null, string? previousRefresh = null)
    {
        var now = Clock?.UtcNow ?? DateTimeOffset.UtcNow;
        var id = sessionId ?? device.SessionId;
        var refresh = $"refresh.{userId}.{generation}.{Guid.NewGuid():N}";
        _refresh[refresh] = new IssuedRefresh(userId, generation, previousRefresh);
        _sessions[id] = new RemoteSession
        {
            SessionId = id,
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
                AccessTokenExpiresAt = now + AccessLifetime,
                RefreshTokenExpiresAt = now + RefreshLifetime,
                SessionId = id,
                UserId = userId,
                UserName = userId
            }
        };
    }

    sealed record IssuedRefresh(string UserId, int Generation, string? Previous);
}

sealed class ScriptedHandler : HttpMessageHandler
{
    readonly Func<HttpRequestMessage, int, HttpResponseMessage> _script;

    public ScriptedHandler(Func<HttpRequestMessage, int, HttpResponseMessage> script)
    {
        _script = script;
    }

    public int Calls { get; private set; }

    public List<string?> AuthorizationHeaders { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        AuthorizationHeaders.Add(request.Headers.Authorization?.ToString());
        return Task.FromResult(_script(request, Calls));
    }
}

static class Harness
{
    public static (
        SecureSessionImplementation Session,
        FakeAuthGateway Auth,
        MemorySessionStore Store,
        FakeBiometricGate Biometrics,
        FakeClock Clock,
        SecureSessionOptions Options) Create(Action<SecureSessionOptions>? configure = null)
    {
        var options = new SecureSessionOptions
        {
            AccessTokenRefreshSkew = TimeSpan.FromSeconds(30),
            LockOnBackground = false
        };
        configure?.Invoke(options);

        var clock = new FakeClock();
        var auth = new FakeAuthGateway { Clock = clock };
        var store = new MemorySessionStore();
        var biometrics = new FakeBiometricGate();
        var device = new FakeDeviceIdentity();
        var session = SecureSession.Create(options, auth, store, biometrics, clock, device);
        return (session, auth, store, biometrics, clock, options);
    }

    public static HttpClient CreateClient(ISecureSession session, ScriptedHandler inner, SecureSessionOptions? options = null)
    {
        var handler = new SecureSessionHandler(session, options, inner);
        return new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = new Uri("https://api.test")
        };
    }

    public static string JwtWithExpiry(DateTimeOffset expiresAt)
    {
        var header = Base64Url("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payload = Base64Url($"{{\"exp\":{expiresAt.ToUnixTimeSeconds()}}}");
        return $"{header}.{payload}.";
    }

    static string Base64Url(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
