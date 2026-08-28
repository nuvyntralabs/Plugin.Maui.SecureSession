#if !ANDROID && !IOS
namespace Plugin.Maui.SecureSession;

sealed class PlatformBiometricGate : IBiometricGate
{
    public static IBiometricGate Create() => new PlatformBiometricGate();

    public Task<BiometricAvailability> GetAvailabilityAsync() =>
        Task.FromResult(BiometricAvailability.NotSupported);

    public Task<bool> AuthenticateAsync(string reason, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(false);
    }
}
#endif
