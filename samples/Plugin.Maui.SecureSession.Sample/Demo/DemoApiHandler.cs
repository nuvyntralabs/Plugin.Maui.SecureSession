using System.Net;
using System.Text;

namespace Plugin.Maui.SecureSession.Sample.Demo;

public sealed class DemoApiHandler : DelegatingHandler
{
    int _forceUnauthorized;

    public void ForceNextUnauthorized() => Interlocked.Exchange(ref _forceUnauthorized, 1);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = request.Headers.Authorization?.Parameter;
        if (Interlocked.Exchange(ref _forceUnauthorized, 0) == 1 ||
            string.IsNullOrWhiteSpace(token) ||
            !token.StartsWith("access.", StringComparison.Ordinal))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                RequestMessage = request,
                Content = new StringContent("{\"error\":\"expired\"}", Encoding.UTF8, "application/json")
            });
        }

        var user = token.Split('.').Skip(1).FirstOrDefault() ?? "user";
        var body = $"{{\"ok\":true,\"user\":\"{user}\",\"tokenSuffix\":\"{token[^8..]}\"}}";
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            RequestMessage = request,
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
    }
}
