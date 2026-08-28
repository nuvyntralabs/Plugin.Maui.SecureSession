using System.Text;

namespace Plugin.Maui.SecureSession;

static class JwtExpiry
{
    public static DateTimeOffset? TryReadExpiration(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = PadBase64Url(parts[1]);
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var seconds))
            {
                return DateTimeOffset.FromUnixTimeSeconds(seconds);
            }
        }
        catch (FormatException)
        {
        }
        catch (JsonException)
        {
        }
        catch (ArgumentException)
        {
        }

        return null;
    }

    static string PadBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        return (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            _ => padded
        };
    }
}
