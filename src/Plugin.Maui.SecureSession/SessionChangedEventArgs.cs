namespace Plugin.Maui.SecureSession;

/// <summary>
/// Arguments for <see cref="ISecureSession.SessionChanged"/>.
/// </summary>
public sealed class SessionChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes event arguments.
    /// </summary>
    public SessionChangedEventArgs(SessionState previous, SessionState current, SessionSnapshot? snapshot)
    {
        Previous = previous;
        Current = current;
        Snapshot = snapshot;
    }

    /// <summary>
    /// Gets the state before the change.
    /// </summary>
    public SessionState Previous { get; }

    /// <summary>
    /// Gets the state after the change.
    /// </summary>
    public SessionState Current { get; }

    /// <summary>
    /// Gets the snapshot after the change, or <c>null</c> when anonymous.
    /// </summary>
    public SessionSnapshot? Snapshot { get; }
}
