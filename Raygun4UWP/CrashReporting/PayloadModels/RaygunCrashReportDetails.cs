using System.Collections;
using System.Collections.Generic;

namespace Raygun4UWP
{
  public class RaygunCrashReportDetails
  {
    public string MachineName { get; set; }

    public string GroupingKey { get; set; }

    public string Version { get; set; }

    public RaygunErrorInfo Error { get; set; }

    public RaygunEnvironmentInfo Environment { get; set; }

    public RaygunClientInfo Client { get; set; }

    public IList<string> Tags { get; set; }

    public IDictionary UserCustomData { get; set; }

    public RaygunUserInfo User { get; set; }

    public override string ToString()
    {
      // This exists because Reflection in Xamarin can't seem to obtain the Getter methods unless the getter is used somewhere in the code.
      // The getter of all properties is required to serialize the Raygun messages to JSON.
      return string.Format("[RaygunMessageDetails: MachineName={0}, Version={1}, Error={2}, Environment={3}, Client={4}, Tags={5}, UserCustomData={6}, User={7}]", MachineName, Version, Error, Environment, Client, Tags, UserCustomData, User);
    }
  }
}