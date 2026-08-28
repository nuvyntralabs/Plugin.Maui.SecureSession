namespace Plugin.Maui.SecureSession.Tests;

public sealed class TokenRefreshTests
{
    [Fact]
    public async Task GetAccessToken_refreshes_when_inside_skew()
    {
        var (session, auth, _, _, clock, _) = Harness.Create(options =>
        {
            options.AccessTokenRefreshSkew = TimeSpan.FromSeconds(30);
        });
        auth.AccessLifetime = TimeSpan.FromMinutes(2);

        await session.LoginAsync("ada", "maui");
        var original = await session.GetAccessTokenAsync();
        clock.Advance(TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(40)));

        var refreshed = await session.GetAccessTokenAsync();

        Assert.NotEqual(original, refreshed);
        Assert.Equal(1, auth.RefreshCalls);
        Assert.StartsWith("access.ada.1.", refreshed);
        Assert.Equal(1, session.Current?.RefreshGeneration);
    }

    [Fact]
    public async Task Refresh_rotates_refresh_token()
    {
        var (session, auth, _, _, clock, _) = Harness.Create();
        auth.AccessLifetime = TimeSpan.FromMinutes(1);
        await session.LoginAsync("ada", "maui");
        clock.Advance(TimeSpan.FromMinutes(2));

        var first = await session.RefreshTokensAsync();
        clock.Advance(TimeSpan.FromMinutes(2));
        var second = await session.RefreshTokensAsync();

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        Assert.Equal(2, auth.RefreshCalls);
        Assert.Equal(2, session.Current?.RefreshGeneration);
    }

    [Fact]
    public async Task Concurrent_refresh_is_single_flight()
    {
        var (session, auth, _, _, clock, _) = Harness.Create();
        auth.AccessLifetime = TimeSpan.FromMinutes(1);
        auth.RefreshDelay = TimeSpan.FromMilliseconds(80);
        await session.LoginAsync("ada", "maui");
        clock.Advance(TimeSpan.FromMinutes(2));

        var tasks = Enumerable.Range(0, 8).Select(_ => session.GetAccessTokenAsync()).ToArray();
        var tokens = await Task.WhenAll(tasks);

        Assert.Equal(1, auth.RefreshCalls);
        Assert.True(tokens.All(token => token == tokens[0]));
    }

    [Fact]
    public async Task Refresh_reuse_expires_the_session()
    {
        var (session, auth, _, _, clock, _) = Harness.Create();
        auth.AccessLifetime = TimeSpan.FromMinutes(1);
        await session.LoginAsync("ada", "maui");
        clock.Advance(TimeSpan.FromMinutes(2));

        auth.RefreshOverride = _ => throw new RefreshFailedException(
            "Refresh token reused.",
            RefreshFailureKind.RefreshTokenReused);

        SessionExpiryReason? reason = null;
        session.SessionExpired += (_, args) => reason = args.Reason;

        await Assert.ThrowsAsync<SessionExpiredException>(() => session.RefreshTokensAsync());
        Assert.Equal(SessionExpiryReason.RefreshReuseDetected, reason);
        Assert.Equal(SessionState.Expired, session.State);
        Assert.Null(session.Current);
    }

    [Fact]
    public async Task Fresh_token_does_not_refresh()
    {
        var (session, auth, _, _, _, _) = Harness.Create();
        await session.LoginAsync("ada", "maui");

        var first = await session.GetAccessTokenAsync();
        var second = await session.GetAccessTokenAsync();

        Assert.Equal(first, second);
        Assert.Equal(0, auth.RefreshCalls);
    }
}
