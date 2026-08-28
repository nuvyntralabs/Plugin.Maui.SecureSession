namespace Plugin.Maui.SecureSession;

interface IBiometricGate
{
    Task<BiometricAvailability> GetAvailabilityAsync();

    Task<bool> AuthenticateAsync(string reason, CancellationToken cancellationToken);
}
