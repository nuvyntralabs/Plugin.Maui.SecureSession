namespace Plugin.Maui.SecureSession;

/// <summary>
/// Credentials or provider-specific properties sent to <see cref="IAuthGateway"/>.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// Gets the user name, email, or login identifier.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets the password. Not persisted by the plugin.
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// Gets an optional provider hint such as <c>password</c>, <c>otp</c>, or <c>oauth</c>.
    /// </summary>
    public string? Provider { get; init; }

    /// <summary>
    /// Gets extra fields the gateway may need (OTP, client id, scopes).
    /// </summary>
    public IReadOnlyDictionary<string, string>? Properties { get; init; }

    /// <summary>
    /// Creates a password login request.
    /// </summary>
    public static LoginRequest WithPassword(string username, string password) =>
        new()
        {
            Username = username,
            Password = password,
            Provider = "password"
        };
}
