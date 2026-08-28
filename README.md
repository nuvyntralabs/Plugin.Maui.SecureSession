# Plugin.Maui.SecureSession

Mobile authentication and session management for **.NET MAUI** on **iOS** and **Android**.

This package sits one level above [SecureStoragePlus](https://www.nuget.org/packages/Plugin.Maui.SecureStoragePlus). Tokens are persisted there. The session owns login, refresh, expiry, logout, multi-device control, and biometric unlock.

[![NuGet](https://img.shields.io/nuget/v/Plugin.Maui.SecureSession.svg?label=NuGet)](https://www.nuget.org/packages/Plugin.Maui.SecureSession)

```csharp
await session.LoginAsync("ada", "maui");
var token = await session.GetAccessTokenAsync();
```

Automatically:

```
API request
   ↓
401
   ↓
Refresh token
   ↓
New access token
   ↓
Retry
```

## Features

| Feature | What it does |
| --- | --- |
| **Access token** | Issued on login, attached as `Bearer` on `HttpClient` |
| **Refresh token** | Exchanged before expiry or after a 401 |
| **Rotating refresh** | New refresh token replaces the previous one; reuse expires the session |
| **Token expiry** | Uses `expires_at` or a JWT `exp` claim, with a refresh skew |
| **Automatic refresh** | Single-flight refresh shared by concurrent callers |
| **Logout** | This device or every device |
| **Session expiry** | Absolute lifetime and idle timeout |
| **Multi-device** | List and revoke other sessions |
| **Biometric unlock** | Face ID / fingerprint gate after lock or process death |
| **Secure persistence** | AES-256-GCM via `Plugin.Maui.SecureStoragePlus` |

## Install

```bash
dotnet add package Plugin.Maui.SecureSession
```

Target frameworks: `net10.0`, `net10.0-android` (API 23+), `net10.0-ios` (iOS 15+).

## Quick start

```csharp
using Plugin.Maui.SecureSession;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<IAuthGateway, ShopAuthGateway>();

        builder.Services
            .AddHttpClient<IShopApi, ShopApi>(client =>
            {
                client.BaseAddress = new Uri("https://api.shop");
            })
            .AddSecureSession();

        builder
            .UseMauiApp<App>()
            .UseSecureSession(options =>
            {
                options.AccessTokenRefreshSkew = TimeSpan.FromSeconds(60);
                options.IdleTimeout = TimeSpan.FromMinutes(15);
                options.AbsoluteSessionLifetime = TimeSpan.FromDays(14);
                options.RequireBiometricUnlock = true;
                options.LockOnBackground = true;
            });

        return builder.Build();
    }
}
```

```csharp
await session.LoginAsync("ada", "secret");

var token = await session.GetAccessTokenAsync();
```

Resolve `ISecureSession` from DI, or use `SecureSession.Current`.

## Auth gateway

The plugin does not talk to a specific identity server. Your app implements `IAuthGateway` (or sets delegates on `SecureSessionOptions`).

```csharp
public sealed class ShopAuthGateway : IAuthGateway
{
    public bool CanRefresh => true;

    public async Task<AuthResponse> LoginAsync(LoginRequest request, DeviceContext device, CancellationToken ct)
    {
        var tokens = await api.LoginAsync(request.Username, request.Password, device, ct);
        return new AuthResponse { Tokens = tokens };
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, DeviceContext device, CancellationToken ct)
    {
        var tokens = await api.RefreshAsync(request.RefreshToken, device, ct);
        return new AuthResponse { Tokens = tokens, RefreshTokenRotated = true };
    }

    public Task LogoutAsync(LogoutRequest request, CancellationToken ct) =>
        api.LogoutAsync(request, ct);

    public Task<IReadOnlyList<RemoteSession>> GetSessionsAsync(string accessToken, CancellationToken ct) =>
        api.GetSessionsAsync(accessToken, ct);

    public Task RevokeSessionAsync(string accessToken, string sessionId, CancellationToken ct) =>
        api.RevokeAsync(accessToken, sessionId, ct);
}
```

Already have tokens from a browser OAuth flow?

```csharp
await session.LoginAsync(new TokenBundle
{
    AccessToken = access,
    RefreshToken = refresh,
    AccessTokenExpiresAt = expiresAt,
    UserId = userId
});
```

If the access token is a JWT and `AccessTokenExpiresAt` is omitted, the plugin reads the `exp` claim.

When the server rotates refresh tokens, throw `RefreshFailedException` with `RefreshFailureKind.RefreshTokenReused` if a retired token is presented again. The local session is cleared.

## Automatic 401 retry

`AddSecureSession()` wraps `HttpClient` with `SecureSessionHandler`:

1. Attach the current access token
2. Send the request
3. On 401, refresh once (shared across concurrent calls)
4. Retry the original request with the new token

```csharp
builder.Services
    .AddHttpClient("shop", client => client.BaseAddress = new Uri("https://api.shop"))
    .AddSecureSession();
```

Without the generic host:

```csharp
using var client = SecureSessionHttp.CreateClient(session);
```

`GetAccessTokenAsync()` also refreshes proactively when the access token is inside `AccessTokenRefreshSkew`.

## Logout and expiry

```csharp
await session.LogoutAsync();                       // this device
await session.LogoutAsync(LogoutScope.AllDevices); // every device
```

A session also ends when:

- The access token expires and refresh is impossible or rejected
- A rotated refresh token is reused
- `AbsoluteSessionLifetime` elapses
- `IdleTimeout` elapses with no `TouchAsync` / token use

Subscribe to `SessionExpired` to send the user back to login.

## Multi-device sessions

Each login carries a stable `DeviceId` (persisted in SecureStoragePlus) and a new `SessionId`. The gateway can register that pair.

```csharp
var devices = await session.GetSessionsAsync();
await session.RevokeSessionAsync(other.SessionId);
```

Revoking the current session signs out locally.

## Biometric unlock

Tokens stay encrypted at rest. The lock is an in-memory gate: after process death or `LockAsync()`, `GetAccessTokenAsync()` fails with `SessionLockedException` until `UnlockAsync()`.

```csharp
if (await session.GetBiometricAvailabilityAsync() == BiometricAvailability.Available)
{
    await session.EnableBiometricUnlockAsync();
}

await session.LockAsync();
await session.UnlockAsync(); // Face ID / fingerprint
```

`LockOnBackground` locks when the app pauses (Android) or enters the background (iOS), if biometric unlock is enabled.

## Without the generic host

```csharp
var session = SecureSession.Create(new SecureSessionOptions
{
    RefreshAsync = (request, device, ct) => auth.RefreshAsync(request, device, ct),
    LoginAsync = (request, device, ct) => auth.LoginAsync(request, device, ct)
});

await session.RestoreAsync();
```

## Platform notes

**iOS** — Face ID needs a usage string in `Info.plist`:

```xml
<key>NSFaceIDUsageDescription</key>
<string>Unlock your session</string>
```

SecureStoragePlus needs a Keychain entitlement. In `Entitlements.plist`:

```xml
<key>keychain-access-groups</key>
<array>
    <string>$(AppIdentifierPrefix)$(CFBundleIdentifier)</string>
</array>
```

**Android** — BiometricPrompt needs `USE_BIOMETRIC` (declare it on the app) and a minimum of API 23. Enroll a fingerprint or face on the emulator before testing unlock.

| | Android | iOS | `net10.0` |
| --- | --- | --- | --- |
| Login / refresh / logout | Yes | Yes | Yes (tests) |
| 401 refresh + retry | Yes | Yes | Yes |
| SecureStoragePlus persistence | Yes | Yes | Yes |
| Biometric unlock | BiometricPrompt | LocalAuthentication | Stub |
| Lock on background | `OnPause` | `DidEnterBackground` | Call `NotifyBackground` |

## Sample

`samples/Plugin.Maui.SecureSession.Sample` signs in against an in-memory auth server (password `maui`), forces a 401, refreshes, lists devices, and exercises biometric lock.

```bash
dotnet build src/Plugin.Maui.SecureSession/Plugin.Maui.SecureSession.csproj
dotnet pack src/Plugin.Maui.SecureSession/Plugin.Maui.SecureSession.csproj -c Release -o artifacts
dotnet test tests/Plugin.Maui.SecureSession.Tests/Plugin.Maui.SecureSession.Tests.csproj
dotnet build samples/Plugin.Maui.SecureSession.Sample/Plugin.Maui.SecureSession.Sample.csproj -f net10.0-android
```

## Pack from source

```bash
dotnet pack src/Plugin.Maui.SecureSession/Plugin.Maui.SecureSession.csproj -c Release -o artifacts
```

The `.nupkg` is written to `artifacts/Plugin.Maui.SecureSession.1.0.0.nupkg`.

## License

MIT

## Support

> If this plugin saved you a weekend of native plumbing, consider buying me a coffee.
> Your support keeps it maintained, documented, and free.

[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-ffdd00?style=for-the-badge&logo=buy-me-a-coffee&logoColor=black)](https://buymeacoffee.com/npadhy)

This library stays open source. A coffee helps cover time for bug fixes, new features, and docs.
