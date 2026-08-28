namespace Plugin.Maui.SecureSession;

sealed class SecureStoragePlusSessionStore : ISecureSessionStore
{
    readonly ISecureStoragePlus _storage;

    public SecureStoragePlusSessionStore(ISecureStoragePlus storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    public async Task<SessionRecord?> LoadAsync(CancellationToken cancellationToken)
    {
        var json = await _storage.GetAsync(StoreKeys.Session, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize(json, SessionJsonContext.Default.SessionRecord);
    }

    public async Task SaveAsync(SessionRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        var json = JsonSerializer.Serialize(record, SessionJsonContext.Default.SessionRecord);
        await _storage.SetAsync(StoreKeys.Session, json, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        await _storage.RemoveAsync(StoreKeys.Session, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> GetOrCreateDeviceIdAsync(CancellationToken cancellationToken)
    {
        var id = await _storage.GetAsync(StoreKeys.DeviceId, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        id = Guid.NewGuid().ToString("N");
        await _storage.SetAsync(StoreKeys.DeviceId, id, cancellationToken: cancellationToken).ConfigureAwait(false);
        return id;
    }
}
