namespace Plugin.Maui.SecureSession;

/// <summary>
/// Classifies a refresh-token failure so the session can expire or retry.
/// </summary>
public enum RefreshFailureKind
{
    /// <summary>The refresh token is unknown or expired.</summary>
    InvalidRefreshToken,

    /// <summary>A rotated refresh token was presented again (possible theft).</summary>
    RefreshTokenReused,

    /// <summary>The authorization server rejected the caller.</summary>
    Unauthorized,

    /// <summary>A transport or timeout failure. The local session is kept.</summary>
    Network,

    /// <summary>An unclassified failure.</summary>
    Unknown
}
