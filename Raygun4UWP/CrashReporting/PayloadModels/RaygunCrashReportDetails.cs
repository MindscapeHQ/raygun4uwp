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
  }
}