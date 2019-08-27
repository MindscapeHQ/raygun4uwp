using System;

namespace Raygun4UWP
{
  public class RaygunCrashReport
  {
    public RaygunCrashReport()
    {
      OccurredOn = DateTime.UtcNow;
      Details = new RaygunCrashReportDetails();
    }

    public DateTime OccurredOn { get; set; }

    public RaygunCrashReportDetails Details { get; set; }
  }
}