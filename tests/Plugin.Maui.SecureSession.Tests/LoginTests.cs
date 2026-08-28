namespace Plugin.Maui.SecureSession.Tests;

public sealed class LoginTests
{
    [Fact]
    public async Task Login_persists_tokens_and_snapshot()
    {
        var (session, auth, _, _, clock, _) = Harness.Create();

        var snapshot = await session.LoginAsync("ada", "maui");

        Assert.Equal(SessionState.Authenticated, session.State);
        Assert.True(session.IsAuthenticated);
        Assert.Equal("ada", snapshot.UserId);
        Assert.True(snapshot.HasRefreshToken);
        Assert.Equal(clock.UtcNow.AddMinutes(15), snapshot.AccessTokenExpiresAt);
        Assert.Equal(1, auth.LoginCalls);
        Assert.Equal("maui", auth.LastPassword);

        var token = await session.GetAccessTokenAsync();
        Assert.StartsWith("access.ada.0.", token);
    }

    [Fact]
    public async Task Login_with_token_bundle_skips_gateway_login()
    {
        var (session, auth, _, _, clock, _) = Harness.Create();

        var snapshot = await session.LoginAsync(new TokenBundle
        {
            AccessToken = "bundle-access",
            RefreshToken = "bundle-refresh",
            AccessTokenExpiresAt = clock.UtcNow.AddHours(1),
            UserId = "linus",
            UserName = "Linus"
        });

        Assert.Equal(0, auth.LoginCalls);
        Assert.Equal("linus", snapshot.UserId);
        Assert.Equal("bundle-access", await session.GetAccessTokenAsync());
    }

    [Fact]
    public async Task Login_reads_jwt_exp_when_expiry_omitted()
    {
        var (session, _, _, _, clock, _) = Harness.Create();
        var expires = clock.UtcNow.AddMinutes(20);

        var snapshot = await session.LoginAsync(new TokenBundle
        {
            AccessToken = Harness.JwtWithExpiry(expires),
            UserId = "jwt-user"
        });

        Assert.Equal(expires, snapshot.AccessTokenExpiresAt);
    }

    [Fact]
    public async Task Restore_reloads_persisted_session()
    {
        var (first, auth, store, biometrics, clock, options) = Harness.Create();
        await first.LoginAsync("ada", "maui");

        var restored = SecureSession.Create(options, auth, store, biometrics, clock, new FakeDeviceIdentity());
        await restored.RestoreAsync();

        Assert.Equal(SessionState.Authenticated, restored.State);
        Assert.Equal("ada", restored.Current?.UserId);
        Assert.StartsWith("access.ada.0.", await restored.GetAccessTokenAsync());
    }
}
