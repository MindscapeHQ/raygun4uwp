using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Xaml;

namespace Raygun4UWP
{
  internal class RaygunRUMService
  {
    private readonly RaygunSettings _settings;

    private string _sessionId;
    private RaygunUserInfo _userInfo;

    public RaygunRUMService(RaygunSettings settings)
    {
      _settings = settings;
    }

    public string ApplicationVersion { get; set; }

    public RaygunUserInfo UserInfo
    {
      get { return _userInfo; }
      set
      {
        if (_userInfo != value)
        {
          RaygunUserInfo previousUser = _userInfo;

          _userInfo = value;

          if (previousUser != null)
          {
            SendSessionEndEventInternalAsync();
          }
        }
      }
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

    public async Task SendSessionStartEventAsync()
    {
      await SendSessionEndEventAsync();

      _sessionId = Guid.NewGuid().ToString();

      RaygunRUMMessage sessionStartMessage = BuildSessionEventMessage(RaygunRUMEventType.SessionStart, _sessionId);
      
      string payload = JsonConvert.SerializeObject(sessionStartMessage, HttpService.SERIALIZATION_SETTINGS);

      await HttpService.SendRequestAsync(_settings.RealUserMonitoringApiEndpoint, _settings.ApiKey, payload);
    }

    public async Task SendSessionEndEventAsync()
    {
      if (_sessionId != null)
      {
        RaygunRUMMessage sessionStartMessage = BuildSessionEventMessage(RaygunRUMEventType.SessionEnd, _sessionId);

        string payload = JsonConvert.SerializeObject(sessionStartMessage, HttpService.SERIALIZATION_SETTINGS);

        _sessionId = null;

        await HttpService.SendRequestAsync(_settings.RealUserMonitoringApiEndpoint, _settings.ApiKey, payload);
      }
    }

    public async Task SendSessionTimingEventAsync(RaygunRUMEventTimingType type, string name, long milliseconds)
    {
      if (_sessionId == null)
      {
        await SendSessionStartEventAsync();
      }

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

      await HttpService.SendRequestAsync(_settings.RealUserMonitoringApiEndpoint, _settings.ApiKey, payload);
    }

    private async void SendSessionEndEventInternalAsync()
    {
      await SendSessionEndEventAsync();
    }

    private RaygunRUMMessage BuildSessionEventMessage(RaygunRUMEventType eventType, string sessionId)
    {
      var message = new RaygunRUMMessage
      {
        EventData = new RaygunRUMEventInfo[]
        {
          new RaygunRUMEventInfo
          {
            SessionId = sessionId,
            Timestamp = DateTime.UtcNow,
            Type = eventType,
            User = UserInfo ?? DefaultUserService.DefaultUser,
            Version = string.IsNullOrWhiteSpace(ApplicationVersion) ? EnvironmentService.PackageVersion : ApplicationVersion,
            Platform = EnvironmentService.DeviceName,
            OS = EnvironmentService.OperatingSystem,
            OSVersion = EnvironmentService.OperatingSystemVersion
          }
        }
      };

      return message;
    }

    private async void CurrentOnSuspending(object sender, SuspendingEventArgs e)
    {
      await SendSessionEndEventAsync();
    }

    private async void CurrentOnResuming(object sender, object e)
    {
      await SendSessionStartEventAsync();
    }
  }
}
