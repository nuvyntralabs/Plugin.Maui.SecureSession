namespace Plugin.Maui.SecureSession;

/// <summary>
/// Identity of this installation, attached to login and refresh so the server can track devices.
/// </summary>
public sealed class DeviceContext
{
    /// <summary>
    /// Gets the stable installation id persisted in secure storage.
    /// </summary>
    public required string DeviceId { get; init; }

    /// <summary>
    /// Gets the session id for this login.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Gets a human-readable device name.
    /// </summary>
    public required string DeviceName { get; init; }

    /// <summary>
    /// Gets the platform name (<c>Android</c>, <c>iOS</c>).
    /// </summary>
    public required string Platform { get; init; }
}
