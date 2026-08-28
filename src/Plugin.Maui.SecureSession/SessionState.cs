namespace Plugin.Maui.SecureSession;

/// <summary>
/// Lifecycle state of a secure session.
/// </summary>
public enum SessionState
{
    /// <summary>No persisted credentials.</summary>
    Anonymous,

    /// <summary>Signed in and unlocked.</summary>
    Authenticated,

    /// <summary>Signed in, but biometric unlock is required before tokens can be used.</summary>
    Locked,

    /// <summary>A previous session was invalidated and tokens were cleared.</summary>
    Expired
}
