using System;
using System.Collections.Generic;
using System.Reflection;

namespace Raygun4UWP
{
  public class RaygunSettings
  {
    private const string DEFAULT_CR_API_ENDPOINT = "https://api.raygun.com/entries";

    public RaygunSettings(string apiKey)
    {
      ApiKey = apiKey;

      CrashReportingApiEndpoint = new Uri(DEFAULT_CR_API_ENDPOINT);

      StrippedWrapperExceptions = new List<Type> {typeof(TargetInvocationException)};
    }

    public string ApiKey { get; set; }

    public Uri CrashReportingApiEndpoint { get; set; }

    /// <summary>
    /// A list of outer exceptions that will be stripped, leaving only the valuable inner exception.
    /// TargetInvocationException is included in this list by default.
    /// </summary>
    public IList<Type> StrippedWrapperExceptions { get; set; }
  }
}