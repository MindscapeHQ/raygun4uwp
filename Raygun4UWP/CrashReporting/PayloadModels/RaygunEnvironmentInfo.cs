namespace Raygun4UWP
{
  public class RaygunEnvironmentInfo
  {
    public string OSVersion { get; set; }

    public double WindowBoundsWidth { get; set; }

    public double WindowBoundsHeight { get; set; }

    public string CurrentOrientation { get; set; }

    public string Architecture { get; set; }

    public string DeviceManufacturer { get; set; }

    public string DeviceName { get; set; }

    public double UtcOffset { get; set; }

    public string Locale { get; set; }
  }
}