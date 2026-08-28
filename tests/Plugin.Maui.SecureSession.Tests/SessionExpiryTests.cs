namespace Plugin.Maui.SecureSession.Tests;

public sealed class SessionExpiryTests
{
    [Fact]
    public async Task Absolute_lifetime_expires_the_session()
    {
        var (session, _, _, _, clock, _) = Harness.Create(options =>
        {
            options.AbsoluteSessionLifetime = TimeSpan.FromHours(8);
        });
        await session.LoginAsync("ada", "maui");
        clock.Advance(TimeSpan.FromHours(8));

        var expired = await Assert.ThrowsAsync<SessionExpiredException>(() => session.GetAccessTokenAsync());
        Assert.Equal(SessionExpiryReason.AbsoluteLifetime, expired.Reason);
        Assert.Equal(SessionState.Expired, session.State);
    }

    [Fact]
    public async Task Idle_timeout_expires_the_session()
    {
        var (session, _, _, _, clock, _) = Harness.Create(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(15);
        });
        await session.LoginAsync("ada", "maui");
        clock.Advance(TimeSpan.FromMinutes(15));

        var expired = await Assert.ThrowsAsync<SessionExpiredException>(() => session.GetAccessTokenAsync());
        Assert.Equal(SessionExpiryReason.IdleTimeout, expired.Reason);
    }

    [Fact]
    public async Task Touch_extends_idle_window()
    {
        var (session, _, _, _, clock, _) = Harness.Create(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(15);
        });
        await session.LoginAsync("ada", "maui");
        clock.Advance(TimeSpan.FromMinutes(10));
        await session.TouchAsync();
        clock.Advance(TimeSpan.FromMinutes(10));

        var token = await session.GetAccessTokenAsync();
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(SessionState.Authenticated, session.State);
    }

    [Fact]
    public async Task Expired_access_without_refresh_clears_session()
    {
        var (session, auth, _, _, clock, _) = Harness.Create();
        auth.CanRefresh = false;
        await session.LoginAsync(new TokenBundle
        {
            AccessToken = "short-lived",
            AccessTokenExpiresAt = clock.UtcNow.AddMinutes(1),
            UserId = "ada"
        });
        clock.Advance(TimeSpan.FromMinutes(2));

        await Assert.ThrowsAsync<SessionExpiredException>(() => session.GetAccessTokenAsync());
        Assert.Equal(SessionState.Expired, session.State);
    }
}
