using System;
using System.Diagnostics;
using System.Globalization;
using Windows.ApplicationModel;
using Windows.UI.Xaml;

namespace Raygun4UWP
{
  public static class RaygunEnvironmentInfoBuilder
  {
    public static RaygunEnvironmentInfo Build()
    {
      RaygunEnvironmentInfo environmentInfo = new RaygunEnvironmentInfo();

      try
      {
        if (Window.Current != null)
        {
          environmentInfo.WindowBoundsWidth = Window.Current.Bounds.Width;
          environmentInfo.WindowBoundsHeight = Window.Current.Bounds.Height;

          var sensor = Windows.Devices.Sensors.SimpleOrientationSensor.GetDefault();

          if (sensor != null)
          {
            environmentInfo.CurrentOrientation = sensor.GetCurrentOrientation().ToString();
          }
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error retrieving screen info: {ex.Message}");
      }

      try
      {
        DateTime now = DateTime.Now;
        environmentInfo.UtcOffset = TimeZoneInfo.Local.GetUtcOffset(now).TotalHours;
        environmentInfo.Locale = CultureInfo.CurrentCulture.DisplayName;
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error retrieving time and locale: {ex.Message}");
      }

      try
      {
        environmentInfo.DeviceManufacturer = EnvironmentService.DeviceManufacturer;
        environmentInfo.DeviceName = EnvironmentService.DeviceName;
        environmentInfo.OSName = EnvironmentService.OperatingSystem;
        environmentInfo.OSVersion = EnvironmentService.OperatingSystemVersion;
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error retrieving device info: {ex.Message}");
      }

      try
      {
        Package package = Package.Current;
        environmentInfo.Architecture = package.Id.Architecture.ToString();
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error retrieving package architecture: {ex.Message}");
      }

      return environmentInfo;
    }
  }
}
