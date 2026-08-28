namespace Plugin.Maui.SecureSession;

sealed class MauiDeviceIdentity : IDeviceIdentity
{
    public string GetDeviceName()
    {
        try
        {
            return DeviceInfo.Current.Name;
        }
        catch
        {
            return "Unknown device";
        }
    }

    public string GetPlatform()
    {
        try
        {
            return DeviceInfo.Current.Platform.ToString();
        }
        catch
        {
            return "Unknown";
        }
    }
}
