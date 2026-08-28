namespace Plugin.Maui.SecureSession;

/// <summary>
/// Thrown when tokens are persisted but the session is locked pending biometric unlock.
/// </summary>
public sealed class SessionLockedException : SecureSessionException
{
    /// <summary>
    /// Initializes a new exception.
    /// </summary>
    public SessionLockedException(string? message = null)
        : base(message ?? "The session is locked. Call UnlockAsync before reading tokens.")
    {
    }
}
