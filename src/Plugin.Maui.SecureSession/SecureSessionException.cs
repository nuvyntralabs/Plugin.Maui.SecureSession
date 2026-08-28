namespace Plugin.Maui.SecureSession;

/// <summary>
/// Base exception for session operations.
/// </summary>
public class SecureSessionException : Exception
{
    /// <summary>
    /// Initializes a new exception.
    /// </summary>
    public SecureSessionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new exception with an inner exception.
    /// </summary>
    public SecureSessionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
