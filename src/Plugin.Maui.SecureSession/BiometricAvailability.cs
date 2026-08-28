namespace Plugin.Maui.SecureSession;

/// <summary>
/// Whether the device can authenticate the user with biometrics.
/// </summary>
public enum BiometricAvailability
{
    /// <summary>A biometric prompt can be shown.</summary>
    Available,

    /// <summary>Hardware exists but nothing is enrolled.</summary>
    NotEnrolled,

    /// <summary>The platform does not support biometrics.</summary>
    NotSupported,

    /// <summary>Hardware or OS policy currently prevents a prompt.</summary>
    Unavailable
}
