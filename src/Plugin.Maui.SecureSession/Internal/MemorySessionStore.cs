namespace Plugin.Maui.SecureSession;

sealed class MemorySessionStore : ISecureSessionStore
{
    SessionRecord? _record;
    string? _deviceId;

    public Task<SessionRecord?> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_record?.Clone());
    }

    public Task SaveAsync(SessionRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();
        _record = record.Clone();
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _record = null;
        return Task.CompletedTask;
    }

    public Task<string> GetOrCreateDeviceIdAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _deviceId ??= Guid.NewGuid().ToString("N");
        return Task.FromResult(_deviceId);
    }
}
