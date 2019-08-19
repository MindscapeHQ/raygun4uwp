using System.ComponentModel;

namespace Raygun4UWP
{
  /// <summary>
  /// Can be used to modify the message before sending, or to cancel the send operation.
  /// </summary>
  public class RaygunSendingMessageEventArgs : CancelEventArgs
  {
    public RaygunSendingMessageEventArgs(RaygunCrashReport message)
    {
      Message = message;
    }

    public RaygunCrashReport Message { get; private set; }
  }
}