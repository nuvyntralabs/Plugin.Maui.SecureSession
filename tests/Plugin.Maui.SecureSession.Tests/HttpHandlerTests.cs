using System.Net;

namespace Plugin.Maui.SecureSession.Tests;

public sealed class HttpHandlerTests
{
    [Fact]
    public async Task Attaches_bearer_and_retries_after_401_refresh()
    {
        var (session, auth, _, _, _, options) = Harness.Create();
        auth.AccessLifetime = TimeSpan.FromHours(1);
        await session.LoginAsync("ada", "maui");
        var original = await session.GetAccessTokenAsync();

        var inner = new ScriptedHandler((request, call) =>
        {
            var token = request.Headers.Authorization?.Parameter;
            if (call == 1 && token == original)
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var client = Harness.CreateClient(session, inner, options);
        var response = await client.GetAsync("/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, inner.Calls);
        Assert.Equal(1, auth.RefreshCalls);
        Assert.Equal($"Bearer {original}", inner.AuthorizationHeaders[0]);
        Assert.NotEqual(inner.AuthorizationHeaders[0], inner.AuthorizationHeaders[1]);
    }

    [Fact]
    public async Task Skips_second_refresh_when_token_already_rotated()
    {
        var (session, auth, _, _, clock, options) = Harness.Create();
        auth.AccessLifetime = TimeSpan.FromMinutes(1);
        await session.LoginAsync("ada", "maui");

        var inner = new ScriptedHandler((_, call) =>
        {
            if (call == 1)
            {
                clock.Advance(TimeSpan.FromMinutes(2));
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var client = Harness.CreateClient(session, inner, options);
        var response = await client.GetAsync("/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, auth.RefreshCalls);
    }
}
