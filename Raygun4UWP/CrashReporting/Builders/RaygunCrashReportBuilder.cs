using System;
using System.Collections;
using System.Collections.Generic;

namespace Raygun4UWP
{
  public class RaygunCrashReportBuilder : IRaygunCrashReportBuilder
  {
    private readonly RaygunCrashReport _raygunCrashReport;

    public static IRaygunCrashReportBuilder New
    {
      get
      {
        return new RaygunCrashReportBuilder();
      }
    }

    private RaygunCrashReportBuilder()
    {
      _raygunCrashReport = new RaygunCrashReport();
    }

    public RaygunCrashReport Build()
    {
      return _raygunCrashReport;
    }

    public IRaygunCrashReportBuilder SetMachineName(string machineName)
    {
      _raygunCrashReport.Details.MachineName = machineName;
      return this;
    }

    public IRaygunCrashReportBuilder SetEnvironmentInfo()
    {
      _raygunCrashReport.Details.Environment = RaygunEnvironmentInfoBuilder.Build();
      return this;
    }

    public IRaygunCrashReportBuilder SetErrorInfo(Exception exception)
    {
      if (exception != null)
      {
        _raygunCrashReport.Details.Error = RaygunErrorInfoBuilder.Build(exception);
      }
      return this;
    }

    public IRaygunCrashReportBuilder SetClientInfo()
    {
      _raygunCrashReport.Details.Client = new RaygunClientInfo();
      return this;
    }

    public IRaygunCrashReportBuilder SetCustomData(IDictionary userCustomData)
    {
      _raygunCrashReport.Details.UserCustomData = userCustomData;
      return this;
    }

    public IRaygunCrashReportBuilder SetTags(IList<string> tags)
    {
      _raygunCrashReport.Details.Tags = tags;
      return this;
    }

    public IRaygunCrashReportBuilder SetUserInfo(RaygunUserInfo user)
    {
      _raygunCrashReport.Details.User = user;
      return this;
    }

    public IRaygunCrashReportBuilder SetVersion(string version)
    {
      _raygunCrashReport.Details.Version = version;
      return this;
    }

    public IRaygunCrashReportBuilder SetOccurredOn(DateTime occurredOn)
    {
      _raygunCrashReport.OccurredOn = occurredOn;
      return this;
    }

    public IRaygunCrashReportBuilder SetBreadcrumbs(IList<RaygunBreadcrumb> raygunBreadcrumbs)
    {
      _raygunCrashReport.Details.Breadcrumbs = raygunBreadcrumbs;
      return this;
    }
  }
}