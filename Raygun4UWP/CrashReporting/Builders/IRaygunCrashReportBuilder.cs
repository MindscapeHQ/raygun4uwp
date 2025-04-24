using System;
using System.Collections;
using System.Collections.Generic;

namespace Raygun4UWP
{
  public interface IRaygunCrashReportBuilder
  {
    RaygunCrashReport Build();

    IRaygunCrashReportBuilder SetMachineName(string machineName);

    IRaygunCrashReportBuilder SetErrorInfo(Exception exception);

    IRaygunCrashReportBuilder SetClientInfo();

    IRaygunCrashReportBuilder SetEnvironmentInfo();

    IRaygunCrashReportBuilder SetVersion(string version);

    IRaygunCrashReportBuilder SetCustomData(IDictionary userCustomData);

    IRaygunCrashReportBuilder SetTags(IList<string> tags);

    IRaygunCrashReportBuilder SetUserInfo(RaygunUserInfo user);

    IRaygunCrashReportBuilder SetOccurredOn(DateTime currentTime);
    IRaygunCrashReportBuilder SetBreadcrumbs(IList<RaygunBreadcrumb> raygunBreadcrumbs);
  }
}