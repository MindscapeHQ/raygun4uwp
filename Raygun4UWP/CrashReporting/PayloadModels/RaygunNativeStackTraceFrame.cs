namespace Raygun4UWP
{
  public class RaygunNativeStackTraceFrame
  {
    public long IP { get; set; }

    public long ImageBase { get; set; }

    public string Raw { get; set; }
  }
}
