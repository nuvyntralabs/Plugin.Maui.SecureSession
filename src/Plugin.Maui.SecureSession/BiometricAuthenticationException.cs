namespace Plugin.Maui.SecureSession;

/// <summary>
/// Thrown when a biometric prompt fails or is unavailable.
/// </summary>
public sealed class BiometricAuthenticationException : SecureSessionException
{
    /// <summary>
    /// Initializes a new exception.
    /// </summary>
    public BiometricAuthenticationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new exception with an inner exception.
    /// </summary>
    public BiometricAuthenticationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
