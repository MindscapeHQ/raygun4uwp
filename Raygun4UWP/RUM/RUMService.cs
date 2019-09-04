using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Raygun4UWP
{
  internal class RUMService
  {
    private readonly RaygunSettings _settings;

    private static readonly Stopwatch _stopwatch = new Stopwatch();

    private bool _isEnabled;
    private string _sessionId;
    private RaygunUserInfo _userInfo;

    public RUMService(RaygunSettings settings)
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

          if (_isEnabled && previousUser != null)
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

      SendSessionStartEventInternalAsync();

      _isEnabled = true;
    }

    public void Disable()
    {
      if (Application.Current != null)
      {
        Application.Current.Resuming -= CurrentOnResuming;
        Application.Current.Suspending -= CurrentOnSuspending;
      }

      _isEnabled = false;
    }

    public async Task SendSessionStartEventAsync()
    {
      await SendSessionEndEventAsync();

      _sessionId = Guid.NewGuid().ToString();

      RaygunRUMMessage sessionStartMessage = BuildSessionEventMessage(RaygunRUMEventType.SessionStart, _sessionId);

      await SendRUMMessageAsync(sessionStartMessage);
    }

    public async Task SendSessionEndEventAsync()
    {
      if (_sessionId != null)
      {
        RaygunRUMMessage sessionEndMessage = BuildSessionEventMessage(RaygunRUMEventType.SessionEnd, _sessionId);

        _sessionId = null;

        await SendRUMMessageAsync(sessionEndMessage);
      }
    }

    public async Task SendSessionTimingEventAsync(RaygunRUMEventTimingType type, string name, long milliseconds)
    {
      if (_sessionId == null)
      {
        await SendSessionStartEventAsync();
      }

      RaygunRUMMessage sessionTimingMessage = BuildSessionEventMessage(RaygunRUMEventType.Timing, _sessionId);

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

      sessionTimingMessage.EventData[0].Data = dataPayload;

      await SendRUMMessageAsync(sessionTimingMessage);
    }

    public void ListenToNavigation(UIElement element)
    {
      if (element is Frame frame)
      {
        frame.Navigating += Frame_OnNavigating;
        frame.Navigated += Frame_OnNavigated;
      }
    }

    public void StopListeningToNavigation(UIElement element)
    {
      if (element is Frame frame)
      {
        frame.Navigating -= Frame_OnNavigating;
        frame.Navigated -= Frame_OnNavigated;
      }
    }

    private static void Frame_OnNavigating(object sender, NavigatingCancelEventArgs e)
    {
      if (RaygunClient.Current != null && e.SourcePageType != null)
      {
        _stopwatch.Restart();
      }
    }

    private static void Frame_OnNavigated(object sender, NavigationEventArgs e)
    {
      if (RaygunClient.Current != null && e.SourcePageType != null)
      {
        _stopwatch.Stop();
        string name = e.SourcePageType.Name;
        RaygunClient.Current.SendSessionTimingEventAsync(RaygunRUMEventTimingType.ViewLoaded, name, _stopwatch.ElapsedMilliseconds);
      }
    }

    private async void SendSessionStartEventInternalAsync()
    {
      await SendSessionStartEventAsync();
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

    private async Task SendRUMMessageAsync(RaygunRUMMessage message)
    {
      if (ValidateApiKey())
      {
        string payload = JsonConvert.SerializeObject(message, HttpService.SERIALIZATION_SETTINGS);

        await HttpService.SendRequestAsync(_settings.RealUserMonitoringApiEndpoint, _settings.ApiKey, payload);
      }
    }

    private bool ValidateApiKey()
    {
      if (string.IsNullOrEmpty(_settings.ApiKey))
      {
        Debug.WriteLine("ApiKey has not been provided, RUM event will not be sent");
        return false;
      }

      return true;
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
