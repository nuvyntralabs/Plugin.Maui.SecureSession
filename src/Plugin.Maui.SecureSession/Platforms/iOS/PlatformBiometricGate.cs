using LocalAuthentication;

namespace Plugin.Maui.SecureSession;

sealed class PlatformBiometricGate : IBiometricGate
{
    public static IBiometricGate Create() => new PlatformBiometricGate();

    public Task<BiometricAvailability> GetAvailabilityAsync()
    {
        using var context = new LAContext();
        if (context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out var error))
        {
            return Task.FromResult(BiometricAvailability.Available);
        }

        var availability = error?.Code switch
        {
            (int)LAStatus.BiometryNotEnrolled => BiometricAvailability.NotEnrolled,
            (int)LAStatus.BiometryNotAvailable => BiometricAvailability.NotSupported,
            (int)LAStatus.PasscodeNotSet => BiometricAvailability.NotEnrolled,
            _ => BiometricAvailability.Unavailable
        };

        return Task.FromResult(availability);
    }

    public async Task<bool> AuthenticateAsync(string reason, CancellationToken cancellationToken)
    {
        using var context = new LAContext();
        if (!context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out var error))
        {
            throw new BiometricAuthenticationException(error?.LocalizedDescription ?? "Biometrics are not available.");
        }

        using var registration = cancellationToken.Register(() => context.Invalidate());
        var result = await context.EvaluatePolicyAsync(LAPolicy.DeviceOwnerAuthenticationWithBiometrics, reason)
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        return result.Item1;
    }
}
