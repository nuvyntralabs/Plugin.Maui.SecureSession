namespace Plugin.Maui.SecureSession.Tests;

public sealed class BiometricTests
{
    [Fact]
    public async Task Locked_session_rejects_token_access()
    {
        var (session, _, _, _, _, _) = Harness.Create(options =>
        {
            options.RequireBiometricUnlock = true;
        });
        await session.LoginAsync("ada", "maui");
        await session.LockAsync();

        Assert.True(session.IsLocked);
        await Assert.ThrowsAsync<SessionLockedException>(() => session.GetAccessTokenAsync());
    }

    [Fact]
    public async Task Unlock_with_biometrics_restores_access()
    {
        var (session, _, _, biometrics, _, _) = Harness.Create();
        await session.LoginAsync("ada", "maui");
        await session.EnableBiometricUnlockAsync();
        await session.LockAsync();

        await session.UnlockAsync();

        Assert.True(session.IsAuthenticated);
        Assert.Equal(2, biometrics.AuthenticateCalls);
        Assert.StartsWith("access.ada.", await session.GetAccessTokenAsync());
    }

    [Fact]
    public async Task Failed_unlock_stays_locked()
    {
        var (session, _, _, biometrics, _, _) = Harness.Create();
        await session.LoginAsync("ada", "maui");
        await session.EnableBiometricUnlockAsync();
        await session.LockAsync();
        biometrics.Succeed = false;

        await Assert.ThrowsAsync<BiometricAuthenticationException>(() => session.UnlockAsync());
        Assert.True(session.IsLocked);
    }

    [Fact]
    public async Task Restore_stays_locked_when_biometric_is_enabled()
    {
        var (first, auth, store, biometrics, clock, options) = Harness.Create();
        await first.LoginAsync("ada", "maui");
        await first.EnableBiometricUnlockAsync();

        var restored = SecureSession.Create(options, auth, store, biometrics, clock, new FakeDeviceIdentity());
        await restored.RestoreAsync();

        Assert.Equal(SessionState.Locked, restored.State);
        await Assert.ThrowsAsync<SessionLockedException>(() => restored.GetAccessTokenAsync());
    }

    [Fact]
    public async Task Background_locks_when_biometric_is_enabled()
    {
        var (session, _, _, _, _, _) = Harness.Create(options =>
        {
            options.LockOnBackground = true;
        });
        await session.LoginAsync("ada", "maui");
        await session.EnableBiometricUnlockAsync();

        session.NotifyBackground();

        Assert.True(session.IsLocked);
    }
}
