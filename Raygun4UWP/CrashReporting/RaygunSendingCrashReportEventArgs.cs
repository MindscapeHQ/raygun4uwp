using System;
using System.ComponentModel;

namespace Raygun4UWP
{
  /// <summary>
  /// Can be used to modify the message before sending, or to cancel the send operation.
  /// </summary>
  public class RaygunSendingCrashReportEventArgs : CancelEventArgs
  {
    public RaygunSendingCrashReportEventArgs(Exception originalException, RaygunCrashReport crashReport)
    {
      OriginalException = originalException;
      CrashReport = crashReport;
    }

    public Exception OriginalException { get; }

    public RaygunCrashReport CrashReport { get; }
  }
}