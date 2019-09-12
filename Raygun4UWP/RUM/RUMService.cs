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

    public async Task<RaygunRUMMessage> SendSessionStartEventAsync()
    {
      RaygunRUMMessage sessionStartMessage = null;

      try
      {
        await SendSessionEndEventAsync();

        _sessionId = Guid.NewGuid().ToString();

        sessionStartMessage = BuildSessionEventMessage(RaygunRUMEventType.SessionStart, _sessionId);

        await SendRUMMessageAsync(sessionStartMessage);
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error sending session-start event to Raygun: {ex.Message}");
      }

      return sessionStartMessage;
    }

    public async Task<RaygunRUMMessage> SendSessionEndEventAsync()
    {
      RaygunRUMMessage sessionEndMessage = null;

      try
      {
        if (_sessionId != null)
        {
          sessionEndMessage = BuildSessionEventMessage(RaygunRUMEventType.SessionEnd, _sessionId);

          _sessionId = null;

          await SendRUMMessageAsync(sessionEndMessage);
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error sending session-end event to Raygun: {ex.Message}");
      }

      return sessionEndMessage;
    }

    public async Task<RaygunRUMMessage> SendSessionTimingEventAsync(RaygunRUMEventTimingType type, string name, long milliseconds)
    {
      RaygunRUMMessage sessionTimingMessage = null;

      try
      {
        if (_sessionId == null)
        {
          await SendSessionStartEventAsync();
        }

        sessionTimingMessage = BuildSessionEventMessage(RaygunRUMEventType.Timing, _sessionId);

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
      catch (Exception ex)
      {
        Debug.WriteLine($"Error sending RUM timing event to Raygun: {ex.Message}");
      }

      return sessionTimingMessage;
    }

    public void ListenToNavigation(UIElement element)
    {
      if (element is Frame frame)
      {
        frame.Navigating += Frame_OnNavigating;
        frame.Navigated += Frame_OnNavigated;
        frame.Loading += Frame_OnLoading;
        frame.Loaded += Frame_OnLoaded;
      }
    }

    public void StopListeningToNavigation(UIElement element)
    {
      if (element is Frame frame)
      {
        frame.Navigating -= Frame_OnNavigating;
        frame.Navigated -= Frame_OnNavigated;
        frame.Loading -= Frame_OnLoading;
        frame.Loaded -= Frame_OnLoaded;
      }
    }

    private void Frame_OnLoading(FrameworkElement sender, object args)
    {
      try
      {
        Frame frame = sender as Frame;

        if (RaygunClient.Current != null && frame.Content != null)
        {
          _stopwatch.Restart();
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error handling frame loading: {ex.Message}");
      }
    }

    private void Frame_OnLoaded(object sender, RoutedEventArgs e)
    {
      try
      {
        Frame frame = sender as Frame;

        if (RaygunClient.Current != null && frame.Content != null)
        {
          _stopwatch.Stop();
          string name = frame.Content.GetType().Name;
          RaygunClient.Current.SendSessionTimingEventAsync(RaygunRUMEventTimingType.ViewLoaded, name, _stopwatch.ElapsedMilliseconds);
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error handling frame loaded: {ex.Message}");
      }
    }

    private static void Frame_OnNavigating(object sender, NavigatingCancelEventArgs e)
    {
      try
      {
        if (RaygunClient.Current != null && e.SourcePageType != null)
        {
          _stopwatch.Restart();
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error handling frame navigating: {ex.Message}");
      }
    }

    private static void Frame_OnNavigated(object sender, NavigationEventArgs e)
    {
      try
      {
        if (RaygunClient.Current != null && e.Content != null)
        {
          _stopwatch.Stop();
          string name = e.Content.GetType().Name;
          RaygunClient.Current.SendSessionTimingEventAsync(RaygunRUMEventTimingType.ViewLoaded, name, _stopwatch.ElapsedMilliseconds);
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error handling frame navigated: {ex.Message}");
      }
    }

    private async void SendSessionStartEventInternalAsync()
    {
      // This is called when RUM is enabled which is typically just as the application is starting up.
      // We delay the first message to give the application a bit of time to initialize,
      // otherwise the message seems to fail to be sent due to a silent issue.
      // As this is asynchronous, this does not delay application start up times.
      await Task.Delay(3000);
      // Check to see that there's no session, in case some other operation already started one during the above delay.
      if (_sessionId == null)
      {
        await SendSessionStartEventAsync();
      }
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
      try
      {
        if (ValidateApiKey())
        {
          string payload = JsonConvert.SerializeObject(message, HttpService.SERIALIZATION_SETTINGS);

          await HttpService.SendRequestAsync(_settings.RealUserMonitoringApiEndpoint, _settings.ApiKey, payload);
        }
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error sending RUM message to Raygun: {ex.Message}");
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
