namespace Plugin.Maui.SecureSession;

interface IDeviceIdentity
{
    string GetDeviceName();

    string GetPlatform();
}
