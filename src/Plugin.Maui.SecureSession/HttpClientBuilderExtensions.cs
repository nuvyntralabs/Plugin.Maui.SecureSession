namespace Plugin.Maui.SecureSession;

/// <summary>
/// Attaches <see cref="SecureSessionHandler"/> to an <see cref="IHttpClientBuilder"/>.
/// </summary>
public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// Adds bearer attachment and a single refresh-and-retry on HTTP 401.
    /// </summary>
    public static IHttpClientBuilder AddSecureSession(this IHttpClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddHttpMessageHandler(sp =>
        {
            var session = sp.GetRequiredService<ISecureSession>();
            var options = sp.GetService<SecureSessionOptions>() ?? new SecureSessionOptions();
            return new SecureSessionHandler(session, options);
        });

        return builder;
    }
}
