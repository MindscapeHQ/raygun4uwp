using System;
using System.Collections.Generic;
using System.Reflection;

namespace Raygun4UWP
{
  /// <summary>
  /// Holds options that affect the behaviour of the Raygun4UWP provider.
  /// These can be changed at any time.
  /// </summary>
  public class RaygunSettings
  {
    private const string DEFAULT_CR_API_ENDPOINT = "https://api.raygun.com/entries";
    private const string DEFAULT_RUM_API_ENDPOINT = "https://api.raygun.com/events";

    public RaygunSettings(string apiKey)
    {
      ApiKey = apiKey;

      CrashReportingApiEndpoint = new Uri(DEFAULT_CR_API_ENDPOINT);
      RealUserMonitoringApiEndpoint = new Uri(DEFAULT_RUM_API_ENDPOINT);

      StrippedWrapperExceptions = new List<Type> {typeof(TargetInvocationException)};
    }

    public string ApiKey { get; set; }

    public Uri CrashReportingApiEndpoint { get; set; }

    public Uri RealUserMonitoringApiEndpoint { get; set; }

    /// <summary>
    /// A list of outer exceptions that will be stripped, leaving only the valuable inner exception.
    /// TargetInvocationException is included in this list by default.
    /// </summary>
    public IList<Type> StrippedWrapperExceptions { get; set; }
  }
}