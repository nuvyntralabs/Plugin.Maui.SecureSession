namespace Plugin.Maui.SecureSession.Tests;

public sealed class MultiDeviceTests
{
    [Fact]
    public async Task GetSessions_includes_current_and_other_devices()
    {
        var (session, auth, _, _, _, _) = Harness.Create();
        await session.LoginAsync("ada", "maui");
        auth.AddDevice("ipad-1", "device-ipad", "Ada's iPad", "iOS");

        var sessions = await session.GetSessionsAsync();

        Assert.Equal(2, sessions.Count);
        Assert.Contains(sessions, item => item.IsCurrent && item.DeviceName == "Pixel Test");
        Assert.Contains(sessions, item => item.SessionId == "ipad-1" && !item.IsCurrent);
    }

    [Fact]
    public async Task Revoke_other_device_leaves_current_signed_in()
    {
        var (session, auth, _, _, _, _) = Harness.Create();
        await session.LoginAsync("ada", "maui");
        auth.AddDevice("ipad-1", "device-ipad", "Ada's iPad", "iOS");

        await session.RevokeSessionAsync("ipad-1");

        Assert.True(session.IsAuthenticated);
        var remaining = await session.GetSessionsAsync();
        Assert.DoesNotContain(remaining, item => item.SessionId == "ipad-1");
    }

    [Fact]
    public async Task Revoke_current_session_signs_out()
    {
        var (session, _, _, _, _, _) = Harness.Create();
        var snapshot = await session.LoginAsync("ada", "maui");

        await session.RevokeSessionAsync(snapshot.SessionId);

        Assert.Equal(SessionState.Anonymous, session.State);
    }
}
