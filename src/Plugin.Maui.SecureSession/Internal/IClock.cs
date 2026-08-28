namespace Plugin.Maui.SecureSession;

interface IClock
{
    DateTimeOffset UtcNow { get; }
}
