namespace Plugin.Maui.SecureSession;

/// <summary>
/// Thrown when a refresh-token exchange fails.
/// </summary>
public sealed class RefreshFailedException : SecureSessionException
{
    /// <summary>
    /// Initializes a new exception.
    /// </summary>
    public RefreshFailedException(string message, RefreshFailureKind kind)
        : base(message)
    {
        Kind = kind;
    }

    /// <summary>
    /// Initializes a new exception with an inner exception.
    /// </summary>
    public RefreshFailedException(string message, RefreshFailureKind kind, Exception innerException)
        : base(message, innerException)
    {
        Kind = kind;
    }

    /// <summary>
    /// Gets the failure classification.
    /// </summary>
    public RefreshFailureKind Kind { get; }
}
