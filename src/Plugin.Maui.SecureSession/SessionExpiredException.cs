namespace Plugin.Maui.SecureSession;

/// <summary>
/// Thrown when the session is no longer valid and tokens have been cleared.
/// </summary>
public sealed class SessionExpiredException : SecureSessionException
{
    /// <summary>
    /// Initializes a new exception.
    /// </summary>
    public SessionExpiredException(SessionExpiryReason reason, string? message = null)
        : base(message ?? $"The session expired ({reason}).")
    {
        Reason = reason;
    }

    /// <summary>
    /// Gets why the session ended.
    /// </summary>
    public SessionExpiryReason Reason { get; }
}
