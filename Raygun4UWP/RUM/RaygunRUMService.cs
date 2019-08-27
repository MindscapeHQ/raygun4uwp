using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Raygun4UWP
{
  internal static class RaygunRUMService
  {
    private static string _sessionId;

    public static void SendSessionStartEvent(Uri endpoint, string apiKey)
    {
      _sessionId = Guid.NewGuid().ToString();

      RaygunRUMMessage sessionStartMessage = BuildSessionEventMessage(RaygunRUMEventType.SessionStart, _sessionId);
      
      string payload = JsonConvert.SerializeObject(sessionStartMessage, HttpService.SERIALIZATION_SETTINGS);

      HttpService.SendRequestAsync(endpoint, apiKey, payload);
    }

    public static void SendSessionTimingEvent(Uri endpoint, string apiKey, RaygunRUMEventTimingType type, string name, long milliseconds)
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

      HttpService.SendRequestAsync(endpoint, apiKey, payload);
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
  }
}
