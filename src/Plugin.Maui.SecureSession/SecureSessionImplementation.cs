namespace Plugin.Maui.SecureSession;

sealed class SecureSessionImplementation : ISecureSession
{
    readonly SecureSessionOptions _options;
    readonly IAuthGateway _auth;
    readonly ISecureSessionStore _store;
    readonly IBiometricGate _biometrics;
    readonly IClock _clock;
    readonly IDeviceIdentity _device;
    readonly SemaphoreSlim _mutex = new(1, 1);
    readonly SemaphoreSlim _refreshGate = new(1, 1);
    readonly object _stateGate = new();

    SessionRecord? _record;
    SessionState _state = SessionState.Anonymous;
    bool _unlocked;
    Task? _restoreTask;
    Task<TokenBundle>? _inFlightRefresh;

    public SecureSessionImplementation(
        SecureSessionOptions options,
        IAuthGateway auth,
        ISecureSessionStore store,
        IBiometricGate biometrics,
        IClock clock,
        IDeviceIdentity device)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _biometrics = biometrics ?? throw new ArgumentNullException(nameof(biometrics));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _device = device ?? throw new ArgumentNullException(nameof(device));
    }

    public SessionState State
    {
        get
        {
            lock (_stateGate)
                return _state;
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            lock (_stateGate)
                return _state == SessionState.Authenticated;
        }
    }

    public bool IsLocked
    {
        get
        {
            lock (_stateGate)
                return _state == SessionState.Locked;
        }
    }

    public SessionSnapshot? Current
    {
        get
        {
            lock (_stateGate)
                return _record is null ? null : ToSnapshot(_record);
        }
    }

    public event EventHandler<SessionChangedEventArgs>? SessionChanged;

    public event EventHandler<SessionExpiredEventArgs>? SessionExpired;

    public event EventHandler? Locked;

    public event EventHandler? Unlocked;

    public Task<SessionSnapshot> LoginAsync(string username, string password, CancellationToken cancellationToken = default) =>
        LoginAsync(LoginRequest.WithPassword(username, password), cancellationToken);

    public async Task<SessionSnapshot> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureRestoredAsync(cancellationToken).ConfigureAwait(false);

        var device = await CreateDeviceContextAsync(newSession: true, cancellationToken).ConfigureAwait(false);
        var response = await _auth.LoginAsync(request, device, cancellationToken).ConfigureAwait(false);
        return await AcceptAuthAsync(response, device, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SessionSnapshot> LoginAsync(TokenBundle tokens, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        if (!_options.AcceptUnvalidatedTokens)
        {
            throw new SecureSessionException(
                "LoginAsync(TokenBundle) is disabled. Set AcceptUnvalidatedTokens to true after the host has validated the tokens, or sign in through IAuthGateway.");
        }

        await EnsureRestoredAsync(cancellationToken).ConfigureAwait(false);

        var device = await CreateDeviceContextAsync(newSession: true, cancellationToken).ConfigureAwait(false);
        var response = new AuthResponse { Tokens = tokens };
        return await AcceptAuthAsync(response, device, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRestoredAsync(cancellationToken).ConfigureAwait(false);
        ThrowIfLocked();

        SessionRecord? record;
        lock (_stateGate)
            record = _record;
        if (record is null)
        {
            throw new SessionExpiredException(SessionExpiryReason.TokenExpired, "No session is signed in.");
        }

        if (IsLifetimeExpired(record))
        {
            await ExpireAsync(LifetimeReason(record), cancellationToken).ConfigureAwait(false);
            throw new SessionExpiredException(LifetimeReason(record));
        }

        TouchMemory(record);

        if (IsAccessTokenFresh(record))
        {
            return record.AccessToken;
        }

        if (CanRefresh(record))
        {
            var refreshed = await RefreshTokensAsync(cancellationToken).ConfigureAwait(false);
            return refreshed.AccessToken;
        }

        await ExpireAsync(SessionExpiryReason.TokenExpired, cancellationToken).ConfigureAwait(false);
        throw new SessionExpiredException(SessionExpiryReason.TokenExpired);
    }

    public async Task<TokenBundle> RefreshTokensAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRestoredAsync(cancellationToken).ConfigureAwait(false);
        ThrowIfLocked();

        Task<TokenBundle> pending;
        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_inFlightRefresh is { IsCompleted: false })
            {
                pending = _inFlightRefresh;
            }
            else
            {
                pending = _inFlightRefresh = RefreshCoreAsync();
            }
        }
        finally
        {
            _refreshGate.Release();
        }

        return await pending.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task LogoutAsync(LogoutScope scope = LogoutScope.ThisDevice, CancellationToken cancellationToken = default)
    {
        await EnsureRestoredAsync(cancellationToken).ConfigureAwait(false);

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var record = _record;
            if (record is not null)
            {
                try
                {
                    await _auth.LogoutAsync(new LogoutRequest
                    {
                        AccessToken = record.AccessToken,
                        RefreshToken = record.RefreshToken,
                        SessionId = record.SessionId,
                        DeviceId = record.DeviceId,
                        Scope = scope
                    }, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Local sign-out still proceeds.
                }
            }

            await ClearLocalAsync(cancellationToken).ConfigureAwait(false);
            SetState(SessionState.Anonymous);
        }
        finally
        {
            _mutex.Release();
        }

        _options.Events.OnLoggedOut?.Invoke(scope);
    }

    public Task RestoreAsync(CancellationToken cancellationToken = default) =>
        EnsureRestoredAsync(cancellationToken);

    public async Task TouchAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRestoredAsync(cancellationToken).ConfigureAwait(false);
        if (_record is null)
        {
            return;
        }

        if (IsLifetimeExpired(_record))
        {
            await ExpireAsync(LifetimeReason(_record), cancellationToken).ConfigureAwait(false);
            return;
        }

        TouchMemory(_record);
        await PersistAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task LockAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRestoredAsync(cancellationToken).ConfigureAwait(false);
        bool shouldLock;
        lock (_stateGate)
        {
            shouldLock = _record is not null && _unlocked;
            if (shouldLock)
            {
                _unlocked = false;
            }
        }

        if (!shouldLock)
        {
            return;
        }

        SetState(SessionState.Locked);
        Locked?.Invoke(this, EventArgs.Empty);
        _options.Events.OnLocked?.Invoke();
    }

    public async Task UnlockAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRestoredAsync(cancellationToken).ConfigureAwait(false);

        SessionRecord? record;
        lock (_stateGate)
        {
            record = _record;
            if (record is not null && _unlocked && _state == SessionState.Authenticated)
            {
                return;
            }
        }

        if (record is null)
        {
            throw new SessionExpiredException(SessionExpiryReason.TokenExpired, "No session is signed in.");
        }

        if (IsLifetimeExpired(record))
        {
            await ExpireAsync(LifetimeReason(record), cancellationToken).ConfigureAwait(false);
            throw new SessionExpiredException(LifetimeReason(record));
        }

        var ok = await _biometrics.AuthenticateAsync(_options.BiometricPromptReason, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            throw new BiometricAuthenticationException("Biometric unlock was cancelled or failed.");
        }

        SessionRecord? current;
        lock (_stateGate)
        {
            current = _record;
            if (current is not null)
            {
                _unlocked = true;
            }
        }

        if (current is null)
        {
            throw new SessionExpiredException(SessionExpiryReason.TokenExpired, "No session is signed in.");
        }

        TouchMemory(current);
        SetState(SessionState.Authenticated);
        Unlocked?.Invoke(this, EventArgs.Empty);
        _options.Events.OnUnlocked?.Invoke();
    }

    public Task<BiometricAvailability> GetBiometricAvailabilityAsync() =>
        _biometrics.GetAvailabilityAsync();

    public async Task EnableBiometricUnlockAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRestoredAsync(cancellationToken).ConfigureAwait(false);
        ThrowIfLocked();
        if (_record is null)
        {
            throw new SecureSessionException("Sign in before enabling biometric unlock.");
        }

        var availability = await _biometrics.GetAvailabilityAsync().ConfigureAwait(false);
        if (availability != BiometricAvailability.Available)
        {
            throw new BiometricAuthenticationException($"Biometrics are not available ({availability}).");
        }

        var ok = await _biometrics.AuthenticateAsync(_options.BiometricPromptReason, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            throw new BiometricAuthenticationException("Biometric enrollment was cancelled or failed.");
        }

        _record.BiometricEnabled = true;
        await PersistAsync(cancellationToken).ConfigureAwait(false);
        SetState(_state);
    }

    public async Task DisableBiometricUnlockAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRestoredAsync(cancellationToken).ConfigureAwait(false);
        ThrowIfLocked();
        if (_record is null)
        {
            return;
        }

        _record.BiometricEnabled = false;
        await PersistAsync(cancellationToken).ConfigureAwait(false);
        SetState(_state);
    }

    public async Task<IReadOnlyList<RemoteSession>> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureRestoredAsync(cancellationToken).ConfigureAwait(false);
        ThrowIfLocked();
        if (_record is null)
        {
            return Array.Empty<RemoteSession>();
        }

        IReadOnlyList<RemoteSession> remote;
        try
        {
            remote = await _auth.GetSessionsAsync(_record.AccessToken, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            remote = Array.Empty<RemoteSession>();
        }

        var currentId = _record.SessionId;
        var mapped = remote
            .Select(session => new RemoteSession
            {
                SessionId = session.SessionId,
                DeviceId = session.DeviceId,
                DeviceName = session.DeviceName,
                Platform = session.Platform,
                CreatedAt = session.CreatedAt,
                LastSeenAt = session.LastSeenAt,
                IsCurrent = string.Equals(session.SessionId, currentId, StringComparison.Ordinal)
            })
            .ToList();

        if (mapped.TrueForAll(session => !session.IsCurrent))
        {
            mapped.Insert(0, ToRemote(_record, isCurrent: true));
        }

        return mapped;
    }

    public async Task RevokeSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        await EnsureRestoredAsync(cancellationToken).ConfigureAwait(false);
        ThrowIfLocked();

        if (_record is not null && string.Equals(sessionId, _record.SessionId, StringComparison.Ordinal))
        {
            await LogoutAsync(LogoutScope.ThisDevice, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (_record is not null)
        {
            await _auth.RevokeSessionAsync(_record.AccessToken, sessionId, cancellationToken).ConfigureAwait(false);
        }

        _options.Events.OnSessionRevoked?.Invoke(sessionId);
    }

    public void NotifyBackground()
    {
        // Do not TouchMemory here — resetting LastActivityAt on background
        // would extend idle timeout every time the app leaves the foreground.
        bool shouldLock;
        lock (_stateGate)
        {
            shouldLock = _options.LockOnBackground && _record is not null && _unlocked && ShouldLock();
            if (shouldLock)
            {
                _unlocked = false;
            }
        }

        if (shouldLock)
        {
            SetState(SessionState.Locked);
            Locked?.Invoke(this, EventArgs.Empty);
            _options.Events.OnLocked?.Invoke();
        }
    }

    public void NotifyForeground()
    {
        if (_record is null)
        {
            return;
        }

        if (IsLifetimeExpired(_record))
        {
            _ = ExpireAsync(LifetimeReason(_record), CancellationToken.None);
        }
    }

    Task EnsureRestoredAsync(CancellationToken cancellationToken)
    {
        var existing = Volatile.Read(ref _restoreTask);
        if (existing is null)
        {
            var created = RestoreCoreAsync();
            existing = Interlocked.CompareExchange(ref _restoreTask, created, null) ?? created;
        }

        return existing.WaitAsync(cancellationToken);
    }

    async Task RestoreCoreAsync()
    {
        var record = await _store.LoadAsync(CancellationToken.None).ConfigureAwait(false);
        if (record is null)
        {
            lock (_stateGate)
            {
                _record = null;
                _unlocked = false;
                _state = SessionState.Anonymous;
            }
            return;
        }

        if (IsLifetimeExpired(record))
        {
            await ExpireAsync(LifetimeReason(record), CancellationToken.None).ConfigureAwait(false);
            return;
        }

        lock (_stateGate)
        {
            _record = record;
            if (record.BiometricEnabled || _options.RequireBiometricUnlock)
            {
                _unlocked = false;
                _state = SessionState.Locked;
            }
            else
            {
                _unlocked = true;
                _state = SessionState.Authenticated;
            }
        }
    }

    async Task<TokenBundle> RefreshCoreAsync()
    {
        await _mutex.WaitAsync().ConfigureAwait(false);
        try
        {
            var record = _record;
            if (record is null)
            {
                throw new SessionExpiredException(SessionExpiryReason.TokenExpired, "No session is signed in.");
            }

            if (IsLifetimeExpired(record))
            {
                await ExpireUnlockedAsync(LifetimeReason(record), CancellationToken.None).ConfigureAwait(false);
                throw new SessionExpiredException(LifetimeReason(record));
            }

            if (!CanRefresh(record))
            {
                await ExpireUnlockedAsync(SessionExpiryReason.TokenExpired, CancellationToken.None).ConfigureAwait(false);
                throw new SessionExpiredException(SessionExpiryReason.TokenExpired);
            }

            var device = new DeviceContext
            {
                DeviceId = record.DeviceId,
                SessionId = record.SessionId,
                DeviceName = record.DeviceName ?? _options.DeviceName ?? _device.GetDeviceName(),
                Platform = record.Platform ?? _device.GetPlatform()
            };

            AuthResponse response;
            try
            {
                response = await _auth.RefreshAsync(new RefreshRequest
                {
                    RefreshToken = record.RefreshToken!,
                    AccessToken = record.AccessToken,
                    SessionId = record.SessionId,
                    DeviceId = record.DeviceId,
                    RefreshGeneration = record.RefreshGeneration
                }, device, CancellationToken.None).ConfigureAwait(false);
            }
            catch (RefreshFailedException ex) when (IsFatalRefresh(ex.Kind))
            {
                var reason = ex.Kind == RefreshFailureKind.RefreshTokenReused
                    ? SessionExpiryReason.RefreshReuseDetected
                    : SessionExpiryReason.RefreshRejected;
                await ExpireUnlockedAsync(reason, CancellationToken.None).ConfigureAwait(false);
                throw new SessionExpiredException(reason, ex.Message);
            }

            ApplyRefresh(record, response.Tokens, response.RefreshTokenRotated);
            TouchMemory(record);
            await _store.SaveAsync(record, CancellationToken.None).ConfigureAwait(false);

            var bundle = ToBundle(record);
            _options.Events.OnTokenRefreshed?.Invoke(bundle);
            return bundle;
        }
        finally
        {
            _mutex.Release();
        }
    }

    async Task<SessionSnapshot> AcceptAuthAsync(AuthResponse response, DeviceContext device, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(response.Tokens);

        if (string.IsNullOrWhiteSpace(response.Tokens.AccessToken))
        {
            throw new SecureSessionException("The auth response did not include an access token.");
        }

        var now = _clock.UtcNow;
        var tokens = response.Tokens;
        var accessExpires = tokens.AccessTokenExpiresAt ?? JwtExpiry.TryReadExpiration(tokens.AccessToken);

        var record = new SessionRecord
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            AccessTokenExpiresAt = accessExpires,
            RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt,
            SessionExpiresAt = tokens.SessionExpiresAt ?? AbsoluteExpiry(now),
            IssuedAt = now,
            LastActivityAt = now,
            SessionId = string.IsNullOrWhiteSpace(tokens.SessionId) ? device.SessionId : tokens.SessionId,
            DeviceId = device.DeviceId,
            DeviceName = device.DeviceName,
            Platform = device.Platform,
            UserId = tokens.UserId,
            UserName = tokens.UserName,
            Claims = tokens.Claims is null ? null : new Dictionary<string, string>(tokens.Claims),
            BiometricEnabled = _options.RequireBiometricUnlock,
            RefreshGeneration = 0
        };

        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            lock (_stateGate)
            {
                _record = record;
                _unlocked = true;
            }
            await _store.SaveAsync(record, cancellationToken).ConfigureAwait(false);
            SetState(SessionState.Authenticated);
        }
        finally
        {
            _mutex.Release();
        }

        var snapshot = ToSnapshot(record);
        _options.Events.OnLoggedIn?.Invoke(snapshot);
        return snapshot;
    }

    void ApplyRefresh(SessionRecord record, TokenBundle tokens, bool rotated)
    {
        if (string.IsNullOrWhiteSpace(tokens.AccessToken))
        {
            throw new RefreshFailedException("The refresh response did not include an access token.", RefreshFailureKind.Unknown);
        }

        var now = _clock.UtcNow;
        record.AccessToken = tokens.AccessToken;
        record.AccessTokenExpiresAt = tokens.AccessTokenExpiresAt ?? JwtExpiry.TryReadExpiration(tokens.AccessToken);
        record.RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt ?? record.RefreshTokenExpiresAt;
        record.SessionExpiresAt = tokens.SessionExpiresAt ?? record.SessionExpiresAt;
        record.UserId = tokens.UserId ?? record.UserId;
        record.UserName = tokens.UserName ?? record.UserName;
        if (tokens.Claims is not null)
        {
            record.Claims = new Dictionary<string, string>(tokens.Claims);
        }

        if (!string.IsNullOrWhiteSpace(tokens.RefreshToken) &&
            (_options.RotateRefreshTokens || rotated) &&
            !string.Equals(tokens.RefreshToken, record.RefreshToken, StringComparison.Ordinal))
        {
            record.PreviousRefreshToken = record.RefreshToken;
            record.PreviousRefreshTokenValidUntil = now + _options.RefreshReuseGrace;
            record.RefreshToken = tokens.RefreshToken;
            record.RefreshGeneration++;
        }
        else if (!string.IsNullOrWhiteSpace(tokens.RefreshToken))
        {
            record.RefreshToken = tokens.RefreshToken;
        }
    }

    async Task ExpireAsync(SessionExpiryReason reason, CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await ExpireUnlockedAsync(reason, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }

    async Task ExpireUnlockedAsync(SessionExpiryReason reason, CancellationToken cancellationToken)
    {
        await ClearLocalAsync(cancellationToken).ConfigureAwait(false);
        SetState(SessionState.Expired);
        SessionExpired?.Invoke(this, new SessionExpiredEventArgs(reason));
        _options.Events.OnExpired?.Invoke(reason);
    }

    async Task ClearLocalAsync(CancellationToken cancellationToken)
    {
        await _store.ClearAsync(cancellationToken).ConfigureAwait(false);
        lock (_stateGate)
        {
            _record = null;
            _unlocked = false;
        }
        _inFlightRefresh = null;
    }

    Task PersistAsync(CancellationToken cancellationToken) =>
        _record is null ? Task.CompletedTask : _store.SaveAsync(_record, cancellationToken);

    async Task<DeviceContext> CreateDeviceContextAsync(bool newSession, CancellationToken cancellationToken)
    {
        var deviceId = _record?.DeviceId ?? await _store.GetOrCreateDeviceIdAsync(cancellationToken).ConfigureAwait(false);
        return new DeviceContext
        {
            DeviceId = deviceId,
            SessionId = newSession ? Guid.NewGuid().ToString("N") : _record?.SessionId ?? Guid.NewGuid().ToString("N"),
            DeviceName = _options.DeviceName ?? _device.GetDeviceName(),
            Platform = _device.GetPlatform()
        };
    }

    DateTimeOffset? AbsoluteExpiry(DateTimeOffset now) =>
        _options.AbsoluteSessionLifetime is { } life ? now + life : null;

    void TouchMemory(SessionRecord record) => record.LastActivityAt = _clock.UtcNow;

    bool IsAccessTokenFresh(SessionRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.AccessToken))
        {
            return false;
        }

        if (record.AccessTokenExpiresAt is { } expires)
        {
            return _clock.UtcNow + _options.AccessTokenRefreshSkew < expires;
        }

        // Opaque tokens / JWTs without exp: assume a one-hour lifetime so they
        // are refreshed via the configured skew instead of staying forever-fresh.
        if (CanRefresh(record))
        {
            return _clock.UtcNow + _options.AccessTokenRefreshSkew < record.IssuedAt + TimeSpan.FromHours(1);
        }

        return true;
    }

    bool CanRefresh(SessionRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.RefreshToken) || !_auth.CanRefresh)
        {
            return false;
        }

        return record.RefreshTokenExpiresAt is not { } expires || _clock.UtcNow < expires;
    }

    bool IsLifetimeExpired(SessionRecord record)
    {
        if (record.SessionExpiresAt is { } sessionExpires && _clock.UtcNow >= sessionExpires)
        {
            return true;
        }

        return _options.IdleTimeout is { } idle && _clock.UtcNow - record.LastActivityAt >= idle;
    }

    SessionExpiryReason LifetimeReason(SessionRecord record)
    {
        if (record.SessionExpiresAt is { } sessionExpires && _clock.UtcNow >= sessionExpires)
        {
            return SessionExpiryReason.AbsoluteLifetime;
        }

        return SessionExpiryReason.IdleTimeout;
    }

    bool ShouldLock() => _record?.BiometricEnabled == true || _options.RequireBiometricUnlock;

    void ThrowIfLocked()
    {
        lock (_stateGate)
        {
            if (_state == SessionState.Locked || (_record is not null && !_unlocked && ShouldLock()))
            {
                throw new SessionLockedException();
            }
        }
    }

    static bool IsFatalRefresh(RefreshFailureKind kind) =>
        kind is RefreshFailureKind.InvalidRefreshToken
            or RefreshFailureKind.RefreshTokenReused
            or RefreshFailureKind.Unauthorized;

    void SetState(SessionState next)
    {
        SessionState previous;
        SessionSnapshot? snapshot;
        lock (_stateGate)
        {
            previous = _state;
            _state = next;
            snapshot = _record is null ? null : ToSnapshot(_record);
        }

        if (previous == next && next is not SessionState.Authenticated)
        {
            return;
        }

        SessionChanged?.Invoke(this, new SessionChangedEventArgs(previous, next, snapshot));
    }

    SessionSnapshot ToSnapshot(SessionRecord record) =>
        new()
        {
            State = _state,
            SessionId = record.SessionId,
            DeviceId = record.DeviceId,
            DeviceName = record.DeviceName,
            Platform = record.Platform,
            UserId = record.UserId,
            UserName = record.UserName,
            IssuedAt = record.IssuedAt,
            LastActivityAt = record.LastActivityAt,
            AccessTokenExpiresAt = record.AccessTokenExpiresAt,
            RefreshTokenExpiresAt = record.RefreshTokenExpiresAt,
            SessionExpiresAt = record.SessionExpiresAt,
            HasRefreshToken = !string.IsNullOrWhiteSpace(record.RefreshToken),
            BiometricEnabled = record.BiometricEnabled,
            RefreshGeneration = record.RefreshGeneration,
            Claims = record.Claims
        };

    static TokenBundle ToBundle(SessionRecord record) =>
        new()
        {
            AccessToken = record.AccessToken,
            RefreshToken = record.RefreshToken,
            AccessTokenExpiresAt = record.AccessTokenExpiresAt,
            RefreshTokenExpiresAt = record.RefreshTokenExpiresAt,
            SessionExpiresAt = record.SessionExpiresAt,
            SessionId = record.SessionId,
            UserId = record.UserId,
            UserName = record.UserName,
            Claims = record.Claims
        };

    static RemoteSession ToRemote(SessionRecord record, bool isCurrent) =>
        new()
        {
            SessionId = record.SessionId,
            DeviceId = record.DeviceId,
            DeviceName = record.DeviceName,
            Platform = record.Platform,
            CreatedAt = record.IssuedAt,
            LastSeenAt = record.LastActivityAt,
            IsCurrent = isCurrent
        };
}
