namespace Plugin.Maui.SecureSession;

/// <summary>
/// Why a session was invalidated.
/// </summary>
public enum SessionExpiryReason
{
    /// <summary>Access token expired and no usable refresh token remained.</summary>
    TokenExpired,

    /// <summary>The refresh token was rejected by the server.</summary>
    RefreshRejected,

    /// <summary>The server reported that a rotated refresh token was reused.</summary>
    RefreshReuseDetected,

    /// <summary>Absolute session lifetime elapsed.</summary>
    AbsoluteLifetime,

    /// <summary>Idle timeout elapsed with no recorded activity.</summary>
    IdleTimeout,

    /// <summary>The user signed out.</summary>
    Logout,

    /// <summary>This device session was revoked.</summary>
    Revoked
}
