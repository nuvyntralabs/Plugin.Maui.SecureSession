namespace Plugin.Maui.SecureSession;

/// <summary>
/// How far a logout should propagate.
/// </summary>
public enum LogoutScope
{
    /// <summary>End only the session on this device.</summary>
    ThisDevice,

    /// <summary>End every session for the account.</summary>
    AllDevices
}
