namespace Plugin.Maui.SecureSession;

/// <summary>
/// Attaches the session access token and, on 401, refreshes once then retries the request.
/// </summary>
/// <remarks>
/// Flow: API request → 401 → refresh token → new access token → retry.
/// Concurrent 401s share a single refresh.
/// </remarks>
public sealed class SecureSessionHandler : DelegatingHandler
{
    readonly ISecureSession _session;
    readonly SecureSessionOptions _options;

    /// <summary>
    /// Creates a handler around <paramref name="session"/>.
    /// </summary>
    public SecureSessionHandler(ISecureSession session, SecureSessionOptions? options = null, HttpMessageHandler? innerHandler = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _options = options ?? new SecureSessionOptions();
        if (innerHandler is not null)
        {
            InnerHandler = innerHandler;
        }
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tokenBefore = await AttachTokenAsync(request, cancellationToken).ConfigureAwait(false);
        using var first = await request.CloneAsync(cancellationToken).ConfigureAwait(false);
        var response = await base.SendAsync(first, cancellationToken).ConfigureAwait(false);

        if (!ShouldRefresh(response))
        {
            return response;
        }

        try
        {
            var latest = await _session.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            if (string.Equals(latest, tokenBefore, StringComparison.Ordinal))
            {
                latest = (await _session.RefreshTokensAsync(cancellationToken).ConfigureAwait(false)).AccessToken;
            }

            if (string.IsNullOrWhiteSpace(latest))
            {
                return response;
            }

            ApplyToken(request, latest);
            response.Dispose();
            using var retry = await request.CloneAsync(cancellationToken).ConfigureAwait(false);
            return await base.SendAsync(retry, cancellationToken).ConfigureAwait(false);
        }
        catch (SessionExpiredException)
        {
            return response;
        }
        catch (SessionLockedException)
        {
            return response;
        }
    }

    async Task<string?> AttachTokenAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _session.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            ApplyToken(request, token);
            return token;
        }
        catch (SessionExpiredException)
        {
            request.Headers.Authorization = null;
            return null;
        }
        catch (SessionLockedException)
        {
            request.Headers.Authorization = null;
            return null;
        }
    }

    void ApplyToken(HttpRequestMessage request, string? token)
    {
        request.Headers.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue(_options.AuthenticationScheme, token);
    }

    bool ShouldRefresh(HttpResponseMessage response) =>
        _options.UnauthorizedStatusCodes.Contains(response.StatusCode);
}
