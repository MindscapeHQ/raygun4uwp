using System;

namespace Raygun4UWP
{
  public class RaygunSettings
  {
    private const string DEFAULT_CR_API_ENDPOINT = "https://api.raygun.com/entries";

    public RaygunSettings(string apiKey)
    {
      ApiKey = apiKey;

      CrashReportingApiEndpoint = new Uri(DEFAULT_CR_API_ENDPOINT);
    }

    public string ApiKey { get; set; }

    public Uri CrashReportingApiEndpoint { get; set; }
  }
}