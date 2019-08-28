using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raygun4UWP.Common
{
  internal static class EnvironmentService
  {
    public static string GetPackageVersion()
    {
      string version = null;

      try
      {
        var v = Windows.ApplicationModel.Package.Current.Id.Version;
        version = $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Failed to get application package version: {ex.Message}");
      }

      return version;
    }
  }
}
