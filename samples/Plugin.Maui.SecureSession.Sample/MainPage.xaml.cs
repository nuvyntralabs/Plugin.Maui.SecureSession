using Plugin.Maui.SecureSession;
using Plugin.Maui.SecureSession.Sample.Demo;

namespace Plugin.Maui.SecureSession.Sample;

public partial class MainPage : ContentPage
{
    readonly ISecureSession _session;
    readonly IHttpClientFactory _http;
    readonly DemoApiHandler _api;
    readonly DemoAuthGateway _auth;

    public MainPage(ISecureSession session, IHttpClientFactory http, DemoApiHandler api, DemoAuthGateway auth)
    {
        InitializeComponent();
        _session = session;
        _http = http;
        _api = api;
        _auth = auth;
        _session.SessionChanged += (_, _) => MainThread.BeginInvokeOnMainThread(RefreshStatus);
        _session.SessionExpired += (_, args) => MainThread.BeginInvokeOnMainThread(() =>
        {
            Append($"Expired: {args.Reason}");
            RefreshStatus();
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _session.RestoreAsync();
        RefreshStatus();
    }

    async void OnLogin(object? sender, EventArgs e)
    {
        try
        {
            var snapshot = await _session.LoginAsync(UserEntry.Text ?? "ada", PasswordEntry.Text ?? "maui");
            Append($"Signed in {snapshot.UserId} on {snapshot.DeviceName}.");
        }
        catch (Exception ex)
        {
            Append(ex.Message);
        }

        RefreshStatus();
    }

    async void OnGetToken(object? sender, EventArgs e)
    {
        try
        {
            var token = await _session.GetAccessTokenAsync();
            Append($"Access token: {Trim(token)}");
        }
        catch (Exception ex)
        {
            Append(ex.Message);
        }

        RefreshStatus();
    }

    async void OnCallApi(object? sender, EventArgs e)
    {
        try
        {
            _api.ForceNextUnauthorized();
            var client = _http.CreateClient("shop");
            var body = await client.GetStringAsync("/profile");
            Append($"API retry succeeded: {body}");
        }
        catch (Exception ex)
        {
            Append(ex.Message);
        }

        RefreshStatus();
    }

    async void OnRefresh(object? sender, EventArgs e)
    {
        try
        {
            var tokens = await _session.RefreshTokensAsync();
            Append($"Rotated to {Trim(tokens.AccessToken)} (generation { _session.Current?.RefreshGeneration }).");
        }
        catch (Exception ex)
        {
            Append(ex.Message);
        }

        RefreshStatus();
    }

    async void OnLock(object? sender, EventArgs e)
    {
        await _session.LockAsync();
        Append("Session locked.");
        RefreshStatus();
    }

    async void OnUnlock(object? sender, EventArgs e)
    {
        try
        {
            await _session.UnlockAsync();
            Append("Unlocked.");
        }
        catch (Exception ex)
        {
            Append(ex.Message);
        }

        RefreshStatus();
    }

    async void OnEnableBiometric(object? sender, EventArgs e)
    {
        try
        {
            await _session.EnableBiometricUnlockAsync();
            Append("Biometric unlock enabled.");
        }
        catch (Exception ex)
        {
            Append(ex.Message);
        }

        RefreshStatus();
    }

    async void OnListDevices(object? sender, EventArgs e)
    {
        try
        {
            var sessions = await _session.GetSessionsAsync();
            Append(sessions.Count == 0
                ? "No sessions."
                : string.Join(Environment.NewLine, sessions.Select(item =>
                    $"{(item.IsCurrent ? "•" : "○")} {item.DeviceName} ({item.Platform}) {item.SessionId[..8]}")));
        }
        catch (Exception ex)
        {
            Append(ex.Message);
        }
    }

    void OnAddDevice(object? sender, EventArgs e)
    {
        _auth.AddDemoDevice();
        Append("Added Ada's iPad as a remote session.");
    }

    async void OnLogout(object? sender, EventArgs e)
    {
        await _session.LogoutAsync();
        Append("Signed out this device.");
        RefreshStatus();
    }

    async void OnLogoutAll(object? sender, EventArgs e)
    {
        await _session.LogoutAsync(LogoutScope.AllDevices);
        Append("Signed out every device.");
        RefreshStatus();
    }

    void RefreshStatus()
    {
        var snapshot = _session.Current;
        StatusLabel.Text = $"State: {_session.State}";
        DetailLabel.Text = snapshot is null
            ? "Sign in with password maui. Tokens persist in SecureStoragePlus."
            : $"{snapshot.UserName} · device {snapshot.DeviceName} · refresh gen {snapshot.RefreshGeneration} · biometric {(snapshot.BiometricEnabled ? "on" : "off")} · access expires {snapshot.AccessTokenExpiresAt:HH:mm:ss}";
        LoginPanel.IsVisible = snapshot is null;
        SessionPanel.IsVisible = snapshot is not null;
    }

    void Append(string message) =>
        LogLabel.Text = $"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}{LogLabel.Text}";

    static string Trim(string token) =>
        token.Length <= 24 ? token : $"{token[..16]}…{token[^6..]}";
}
