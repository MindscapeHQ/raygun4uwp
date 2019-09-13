using System;

namespace Raygun4UWP
{
  public class RaygunRUMEventInfo
  {
    public RaygunRUMEventInfo()
    {
    }
    
    public string SessionId { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public RaygunRUMEventType Type { get; set; }
    
    public RaygunUserInfo User { get; set; }
    
    public string Version { get; set; }
    
    public string OS { get; set; }
    
    public string OSVersion { get; set; }
    
    public string Platform { get; set; }
    
    public string Data { get; set; }
  }
}
