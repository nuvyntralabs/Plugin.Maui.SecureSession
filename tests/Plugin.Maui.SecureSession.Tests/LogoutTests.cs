namespace Plugin.Maui.SecureSession.Tests;

public sealed class LogoutTests
{
    [Fact]
    public async Task Logout_this_device_clears_local_session()
    {
        var (session, auth, _, _, _, _) = Harness.Create();
        await session.LoginAsync("ada", "maui");

        await session.LogoutAsync();

        Assert.Equal(SessionState.Anonymous, session.State);
        Assert.Null(session.Current);
        Assert.Equal(1, auth.LogoutCalls);
        Assert.Equal(LogoutScope.ThisDevice, auth.LastLogoutScope);
        await Assert.ThrowsAsync<SessionExpiredException>(() => session.GetAccessTokenAsync());
    }

    [Fact]
    public async Task Logout_all_devices_asks_the_gateway()
    {
        var (session, auth, _, _, _, _) = Harness.Create();
        await session.LoginAsync("ada", "maui");

        await session.LogoutAsync(LogoutScope.AllDevices);

        Assert.Equal(LogoutScope.AllDevices, auth.LastLogoutScope);
        Assert.Equal(SessionState.Anonymous, session.State);
    }
}
