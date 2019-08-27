using Newtonsoft.Json;
using System;
using Windows.ApplicationModel;
using Windows.UI.Xaml;

namespace Raygun4UWP
{
  internal class RaygunRUMService
  {
    private readonly RaygunSettings _settings;

    private string _sessionId;

    public RaygunRUMService(RaygunSettings settings)
    {
      _settings = settings;
    }

    public void Enable()
    {
      Disable(); // This is to avoid attaching the same event handlers multiple times

      if (Application.Current != null)
      {
        Application.Current.Resuming += CurrentOnResuming;
        Application.Current.Suspending += CurrentOnSuspending;
      }
    }

    public void Disable()
    {
      if (Application.Current != null)
      {
        Application.Current.Resuming -= CurrentOnResuming;
        Application.Current.Suspending -= CurrentOnSuspending;
      }
    }

    public void SendSessionStartEvent()
    {
      _sessionId = Guid.NewGuid().ToString();

      RaygunRUMMessage sessionStartMessage = BuildSessionEventMessage(RaygunRUMEventType.SessionStart, _sessionId);
      
      string payload = JsonConvert.SerializeObject(sessionStartMessage, HttpService.SERIALIZATION_SETTINGS);

      HttpService.SendRequestAsync(_settings.RealUserMonitoringApiEndpoint, _settings.ApiKey, payload);
    }

    public void SendSessionTimingEvent(RaygunRUMEventTimingType type, string name, long milliseconds)
    {
      RaygunRUMMessage sessionTimingEvent = BuildSessionEventMessage(RaygunRUMEventType.Timing, _sessionId);

      var data = new RaygunRUMTimingData[]
      {
        new RaygunRUMTimingData
        {
          Name = name,
          Timing = new RaygunRUMTimingInfo
          {
            Type = type,
            Duration = milliseconds
          }
        }
      };
      
      string dataPayload = JsonConvert.SerializeObject(data, HttpService.SERIALIZATION_SETTINGS);

      sessionTimingEvent.EventData[0].Data = dataPayload;

      string payload = JsonConvert.SerializeObject(sessionTimingEvent, HttpService.SERIALIZATION_SETTINGS);

      HttpService.SendRequestAsync(_settings.RealUserMonitoringApiEndpoint, _settings.ApiKey, payload);
    }

    private static RaygunRUMMessage BuildSessionEventMessage(RaygunRUMEventType eventType, string sessionId)
    {
      var message = new RaygunRUMMessage
      {
        EventData = new RaygunRUMEventInfo[]
        {
          new RaygunRUMEventInfo
          {
            SessionId = sessionId,
            Timestamp = DateTime.UtcNow,
            Type = eventType
          }
        }
      };

      return message;
    }

    private void CurrentOnSuspending(object sender, SuspendingEventArgs e)
    {
      return;
    }

    private void CurrentOnResuming(object sender, object e)
    {
      SendSessionStartEvent();
    }
  }
}
