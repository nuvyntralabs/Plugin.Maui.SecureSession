namespace Plugin.Maui.SecureSession;

/// <summary>
/// Arguments for <see cref="ISecureSession.SessionExpired"/>.
/// </summary>
public sealed class SessionExpiredEventArgs : EventArgs
{
    /// <summary>
    /// Initializes event arguments.
    /// </summary>
    public SessionExpiredEventArgs(SessionExpiryReason reason)
    {
        Reason = reason;
    }

    /// <summary>
    /// Gets why the session ended.
    /// </summary>
    public SessionExpiryReason Reason { get; }
}
