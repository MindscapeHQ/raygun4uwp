using System.Reflection;

namespace Raygun4UWP
{
  public class RaygunClientInfo
  {
    public RaygunClientInfo()
    {
      Name = "Raygun4UWP";
      Version = GetType().GetTypeInfo().Assembly.GetName().Version.ToString();
      ClientUrl = "https://github.com/MindscapeHQ/raygun4uwp";
    }

    public string Name { get; set; }

    public string Version { get; set; }

    public string ClientUrl { get; set; }
  }
}