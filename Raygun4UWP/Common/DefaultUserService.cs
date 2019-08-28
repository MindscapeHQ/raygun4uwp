using System;
using System.Diagnostics;
using Windows.Storage;

namespace Raygun4UWP
{
  internal static class DefaultUserService
  {
    private const string DEFAULT_USER_ID_KEY = "Raygun4UWPDefaultUserId";

    private static RaygunUserInfo _defaultUser;

    public static RaygunUserInfo DefaultUser
    {
      get
      {
        try
        {
          if (_defaultUser == null)
          {
            ApplicationData.Current.RoamingSettings.Values.TryGetValue(DEFAULT_USER_ID_KEY, out object defaultUserObj);

            if (!(defaultUserObj is string))
            {
              string id = Guid.NewGuid().ToString().Replace("-", string.Empty);
              ApplicationData.Current.RoamingSettings.Values[DEFAULT_USER_ID_KEY] = id;
              defaultUserObj = id;
            }

            _defaultUser = new RaygunUserInfo((string)defaultUserObj) { IsAnonymous = true };
          }
        }
        catch (Exception ex)
        {
          Debug.WriteLine($"Failed to get or generate a default user id: {ex.Message}");
        }

        return _defaultUser;
      }
    }
  }
}
