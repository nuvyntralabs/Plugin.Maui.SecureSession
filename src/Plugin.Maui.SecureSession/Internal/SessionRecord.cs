namespace Plugin.Maui.SecureSession;

sealed class SessionRecord
{
    public string AccessToken { get; set; } = string.Empty;

    public string? RefreshToken { get; set; }

    public DateTimeOffset? AccessTokenExpiresAt { get; set; }

    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    public DateTimeOffset? SessionExpiresAt { get; set; }

    public DateTimeOffset IssuedAt { get; set; }

    public DateTimeOffset LastActivityAt { get; set; }

    public string SessionId { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;

    public string? DeviceName { get; set; }

    public string? Platform { get; set; }

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public bool BiometricEnabled { get; set; }

    public int RefreshGeneration { get; set; }

    public string? PreviousRefreshToken { get; set; }

    public DateTimeOffset? PreviousRefreshTokenValidUntil { get; set; }

    public Dictionary<string, string>? Claims { get; set; }

    public SessionRecord Clone()
    {
        return new SessionRecord
        {
            AccessToken = AccessToken,
            RefreshToken = RefreshToken,
            AccessTokenExpiresAt = AccessTokenExpiresAt,
            RefreshTokenExpiresAt = RefreshTokenExpiresAt,
            SessionExpiresAt = SessionExpiresAt,
            IssuedAt = IssuedAt,
            LastActivityAt = LastActivityAt,
            SessionId = SessionId,
            DeviceId = DeviceId,
            DeviceName = DeviceName,
            Platform = Platform,
            UserId = UserId,
            UserName = UserName,
            BiometricEnabled = BiometricEnabled,
            RefreshGeneration = RefreshGeneration,
            PreviousRefreshToken = PreviousRefreshToken,
            PreviousRefreshTokenValidUntil = PreviousRefreshTokenValidUntil,
            Claims = Claims is null ? null : new Dictionary<string, string>(Claims)
        };
    }
}
