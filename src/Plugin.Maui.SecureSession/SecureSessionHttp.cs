namespace Plugin.Maui.SecureSession;

/// <summary>
/// Builds an <see cref="HttpClient"/> that uses <see cref="SecureSessionHandler"/> without the generic host.
/// </summary>
public static class SecureSessionHttp
{
    /// <summary>
    /// Creates a client that attaches and refreshes session tokens.
    /// </summary>
    public static HttpClient CreateClient(
        ISecureSession session,
        SecureSessionOptions? options = null,
        HttpMessageHandler? innerHandler = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        var handler = new SecureSessionHandler(session, options, innerHandler ?? new HttpClientHandler());
        return new HttpClient(handler, disposeHandler: true);
    }
}
