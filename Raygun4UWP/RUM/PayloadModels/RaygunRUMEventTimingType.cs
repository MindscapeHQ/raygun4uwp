using System.Runtime.Serialization;

namespace Raygun4UWP
{
  public enum RaygunRUMEventTimingType
  {
    [EnumMember(Value = "p")]
    ViewLoaded,

    [EnumMember(Value = "n")]
    NetworkCall
  }
}