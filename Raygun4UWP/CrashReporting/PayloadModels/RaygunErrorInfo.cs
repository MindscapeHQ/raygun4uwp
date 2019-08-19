using System.Collections;

namespace Raygun4UWP
{
  public class RaygunErrorInfo
  {
    public RaygunErrorInfo InnerError { get; set; }

    public RaygunErrorInfo[] InnerErrors { get; set; }

    public IDictionary Data { get; set; }

    public string ClassName { get; set; }

    public string Message { get; set; }

    public RaygunStackTraceFrame[] StackTrace { get; set; }

    public RaygunNativeStackTraceFrame[] NativeStackTrace { get; set; }

    public RaygunImageInfo[] Images { get; set; }
  }
}
