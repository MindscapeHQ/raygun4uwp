using System;
using System.Diagnostics;
using System.Reflection;
using Windows.ApplicationModel;
using Windows.Security.ExchangeActiveSyncProvisioning;

namespace Raygun4UWP
{
  internal static class EnvironmentService
  {
    private const string UNKNOWN_VALUE = "Unknown";

    private static string _packageVersion;
    private static string _deviceManufacturer;
    private static string _deviceName;
    private static string _operatingSystem;
    private static string _operatingSystemVersion;

    public static string PackageVersion
    {
      get
      {
        if (_packageVersion == null)
        {
          try
          {
            PackageVersion v = Package.Current.Id.Version;
            _packageVersion = $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
          }
          catch (Exception ex)
          {
            Debug.WriteLine($"Failed to get application package version: {ex.Message}");
            _packageVersion = UNKNOWN_VALUE;
          }
        }

        return _packageVersion;
      }
    }

    public static string DeviceManufacturer
    {
      get
      {
        if (_deviceManufacturer == null)
        {
          ResolveDeviceInfo();
        }

        return _deviceManufacturer;
      }
    }

    public static string DeviceName
    {
      get
      {
        if (_deviceName == null)
        {
          ResolveDeviceInfo();
        }

        return _deviceName;
      }
    }

    public static string OperatingSystem
    {
      get
      {
        if (_operatingSystem == null)
        {
          ResolveDeviceInfo();
        }

        return _operatingSystem;
      }
    }

    public static string OperatingSystemVersion
    {
      get
      {
        if (_operatingSystemVersion == null)
        {
          ResolveDeviceInfo();
        }

        return _operatingSystemVersion;
      }
    }

    private static void ResolveDeviceInfo()
    {
      try
      {
        var deviceInfo = new EasClientDeviceInformation();

        _deviceManufacturer = deviceInfo.SystemManufacturer;
        if (string.IsNullOrWhiteSpace(_deviceManufacturer))
        {
          _deviceManufacturer = UNKNOWN_VALUE;
        }

        _deviceName = deviceInfo.SystemProductName;
        if (string.IsNullOrWhiteSpace(_deviceName))
        {
          _deviceName = UNKNOWN_VALUE;
        }

        _operatingSystem = deviceInfo.OperatingSystem;
        if (string.IsNullOrWhiteSpace(_operatingSystem))
        {
          _operatingSystem = UNKNOWN_VALUE;
        }

        _operatingSystemVersion = GetOperatingSystemVersion() ?? deviceInfo.OperatingSystem;
        if (string.IsNullOrWhiteSpace(_operatingSystemVersion))
        {
          _operatingSystemVersion = UNKNOWN_VALUE;
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error retrieving device info: {ex.Message}");
        _deviceManufacturer = UNKNOWN_VALUE;
        _deviceName = UNKNOWN_VALUE;
        _operatingSystem = UNKNOWN_VALUE;
        _operatingSystemVersion = UNKNOWN_VALUE;
      }
    }

    private static string GetOperatingSystemVersion()
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

        if (!long.TryParse(familyVersion.ToString(), out var versionBytes))
        {
          return null;
        }

        var uapVersion = new Version((ushort) (versionBytes >> 48),
          (ushort) (versionBytes >> 32),
          (ushort) (versionBytes >> 16),
          (ushort) (versionBytes));

        return uapVersion.ToString();
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error retrieving operating system version: {ex.Message}");
        return null;
      }
    }
  }
}
