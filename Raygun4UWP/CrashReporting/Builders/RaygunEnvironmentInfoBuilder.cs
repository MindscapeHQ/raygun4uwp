using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Windows.ApplicationModel;
using Windows.Security.ExchangeActiveSyncProvisioning;
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
        var deviceInfo = new EasClientDeviceInformation();
        environmentInfo.DeviceManufacturer = deviceInfo.SystemManufacturer;
        environmentInfo.DeviceName = deviceInfo.SystemProductName;
        environmentInfo.OSVersion = GetOSVersion() ?? deviceInfo.OperatingSystem;
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

    private static string GetOSVersion()
    {
      try
      {
        var analyticsInfoType = Type.GetType("Windows.System.Profile.AnalyticsInfo, Windows, ContentType=WindowsRuntime");
        var versionInfoType = Type.GetType("Windows.System.Profile.AnalyticsVersionInfo, Windows, ContentType=WindowsRuntime");

        if (analyticsInfoType == null || versionInfoType == null)
        {
          return null;
        }

        var versionInfoProperty = analyticsInfoType.GetRuntimeProperty("VersionInfo");
        var versionInfo = versionInfoProperty.GetValue(null);
        var versionProperty = versionInfoType.GetRuntimeProperty("DeviceFamilyVersion");
        var familyVersion = versionProperty.GetValue(versionInfo);

        long versionBytes;
        if (!long.TryParse(familyVersion.ToString(), out versionBytes))
        {
          return null;
        }

        var uapVersion = new Version((ushort)(versionBytes >> 48),
            (ushort)(versionBytes >> 32),
            (ushort)(versionBytes >> 16),
            (ushort)(versionBytes));

        return uapVersion.ToString();
      }
      catch
      {
        return null;
      }
    }
  }
}
