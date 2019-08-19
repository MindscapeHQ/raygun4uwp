using System;
using System.Collections;
using System.Collections.Generic;

namespace Raygun4UWP
{
  public class RaygunCrashReportBuilder : IRaygunCrashReportBuilder
  {
    private readonly RaygunCrashReport _raygunMessage;

    public static IRaygunCrashReportBuilder New
    {
      get
      {
        return new RaygunCrashReportBuilder();
      }
    }

    private RaygunCrashReportBuilder()
    {
      _raygunMessage = new RaygunCrashReport();
    }

    public RaygunCrashReport Build()
    {
      return _raygunMessage;
    }

    public IRaygunCrashReportBuilder SetMachineName(string machineName)
    {
      _raygunMessage.Details.MachineName = machineName;
      return this;
    }

    public IRaygunCrashReportBuilder SetEnvironmentInfo()
    {
      _raygunMessage.Details.Environment = RaygunEnvironmentInfoBuilder.Build();
      return this;
    }

    public IRaygunCrashReportBuilder SetErrorInfo(Exception exception)
    {
      if (exception != null)
      {
        _raygunMessage.Details.Error = RaygunErrorInfoBuilder.Build(exception);
      }
      return this;
    }

    public IRaygunCrashReportBuilder SetClientInfo()
    {
      _raygunMessage.Details.Client = new RaygunClientInfo();
      return this;
    }

    public IRaygunCrashReportBuilder SetCustomData(IDictionary userCustomData)
    {
      _raygunMessage.Details.UserCustomData = userCustomData;
      return this;
    }

    public IRaygunCrashReportBuilder SetTags(IList<string> tags)
    {
      _raygunMessage.Details.Tags = tags;
      return this;
    }

    public IRaygunCrashReportBuilder SetUserInfo(RaygunUserInfo user)
    {
      _raygunMessage.Details.User = user;
      return this;
    }

    public IRaygunCrashReportBuilder SetVersion(string version)
    {
      _raygunMessage.Details.Version = version;
      return this;
    }

    public IRaygunCrashReportBuilder SetOccurredOn(DateTime? occurredOn)
    {
      if (occurredOn != null)
      {
        _raygunMessage.OccurredOn = occurredOn.Value;
      }
      return this;
    }
  }
}