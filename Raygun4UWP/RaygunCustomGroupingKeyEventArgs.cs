using System;

namespace Raygun4UWP
{
  /// <summary>
  /// Can be used to create a custom grouping key for exceptions
  /// </summary>
  public class RaygunCustomGroupingKeyEventArgs : EventArgs
  {
    public RaygunCustomGroupingKeyEventArgs(Exception exception, RaygunCrashReport message)
    {
      Exception = exception;
      Message = message;
    }

    public Exception Exception { get; private set; }
    public RaygunCrashReport Message { get; private set; }

    public string CustomGroupingKey { get; set; }
  }
}