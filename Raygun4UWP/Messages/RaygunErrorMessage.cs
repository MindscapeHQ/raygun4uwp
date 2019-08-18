﻿using System.Collections;

namespace Raygun4UWP
{
  public class RaygunErrorMessage
  {
    public RaygunErrorMessage InnerError { get; set; }

    public RaygunErrorMessage[] InnerErrors { get; set; }

    public IDictionary Data { get; set; }

    public string ClassName { get; set; }

    public string Message { get; set; }

    public RaygunErrorStackTraceLineMessage[] StackTrace { get; set; }

    public RaygunErrorNativeStackTraceLineMessage[] NativeStackTrace { get; set; }

    public RaygunImageMessage[] Images { get; set; }
  }
}
