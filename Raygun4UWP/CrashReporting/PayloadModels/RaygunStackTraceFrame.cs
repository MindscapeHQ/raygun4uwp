namespace Raygun4UWP
{
  public class RaygunStackTraceFrame
  {
    public int? LineNumber { get; set; }

    public string ClassName { get; set; }

    public string FileName { get; set; }

    public string MethodName { get; set; }

    public string Raw { get; set; }
  }
}