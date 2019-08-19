using System;

namespace Raygun4UWP
{
  public class RaygunSettings
  {
    private const string DEFAULT_API_ENDPOINT = "https://api.raygun.com/entries";

    private static readonly RaygunSettings _settings = null;

    public static RaygunSettings Settings
    {
      get
      {
        return _settings ?? new RaygunSettings { ApiKey = "", ApiEndpoint = new Uri(DEFAULT_API_ENDPOINT) };
      }
    }

    public string ApiKey { get; set; }

    public Uri ApiEndpoint { get; set; }
  }
}