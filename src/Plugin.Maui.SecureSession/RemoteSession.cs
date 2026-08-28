namespace Plugin.Maui.SecureSession;

/// <summary>
/// A server-known login session, typically one per device.
/// </summary>
public sealed class RemoteSession
{
    /// <summary>
    /// Gets the server session id.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Gets the device id that created the session.
    /// </summary>
    public required string DeviceId { get; init; }

    /// <summary>
    /// Gets the device display name.
    /// </summary>
    public string? DeviceName { get; init; }

    /// <summary>
    /// Gets the platform that created the session.
    /// </summary>
    public string? Platform { get; init; }

    /// <summary>
    /// Gets when the session was created.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>
    /// Gets when the session was last seen by the server.
    /// </summary>
    public DateTimeOffset? LastSeenAt { get; init; }

    /// <summary>
    /// Gets a value indicating whether this entry is the session on the current device.
    /// </summary>
    public bool IsCurrent { get; init; }
}
