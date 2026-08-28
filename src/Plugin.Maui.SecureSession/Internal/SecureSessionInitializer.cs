namespace Plugin.Maui.SecureSession;

sealed class SecureSessionInitializer : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
        var session = services.GetService<ISecureSession>();
        if (session is null)
        {
            return;
        }

        SecureSession.SetCurrent(session);
        _ = session.RestoreAsync();
    }
}
