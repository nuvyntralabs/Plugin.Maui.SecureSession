namespace Plugin.Maui.SecureSession;

interface ISecureSessionStore
{
    Task<SessionRecord?> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(SessionRecord record, CancellationToken cancellationToken);

    Task ClearAsync(CancellationToken cancellationToken);

    Task<string> GetOrCreateDeviceIdAsync(CancellationToken cancellationToken);
}
