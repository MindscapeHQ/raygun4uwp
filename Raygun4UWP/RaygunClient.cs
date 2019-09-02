using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.UI.Xaml;

namespace Raygun4UWP
{
  public class RaygunClient
  {
    private const string OFFLINE_DATA_FOLDER = "Raygun4UWPOfflineCrashReports";

    private readonly RUMService _rumService;

    private bool _handlingRecursiveErrorSending;
    private string _applicationVersion;
    private RaygunUserInfo _userInfo;

    /// <summary>
    /// Creates a new instance of the <see cref="RaygunClient" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunClient(string apiKey)
    {
      Settings = new RaygunSettings(apiKey);
      _rumService = new RUMService(Settings);
    }

    /// <summary>
    /// Initializes a new RaygunClient with the given API key.
    /// The RaygunClient is set on the static Current property, and then returned.
    /// Calling this method a second time does nothing.
    /// </summary>
    /// <param name="apiKey">Your Raygun application API key.</param>
    /// <returns>The initialized RaygunClient instance.</returns>
    public static RaygunClient Initialize(string apiKey)
    {
      if (Current == null)
      {
        Current = new RaygunClient(apiKey);
      }

      return Current;
    }

    /// <summary>
    /// Gets the <see cref="RaygunClient"/> created by the Initialize method.
    /// </summary>
    public static RaygunClient Current { get; private set; }

    /// <summary>
    /// Gets the settings for this RaygunClient instance.
    /// </summary>
    public RaygunSettings Settings { get; }

    /// <summary>
    /// Gets or sets the user identifier string for the currently logged-in user.
    /// </summary>
    public string UserIdentifier
    {
      get { return UserInfo?.Identifier; }
      set { UserInfo = string.IsNullOrWhiteSpace(value) ? null : new RaygunUserInfo(value); }
    }

    /// <summary>
    /// Gets or sets richer data about the currently logged-in user.
    /// </summary>
    public RaygunUserInfo UserInfo
    {
      get { return _userInfo; }
      set
      {
        _userInfo = value;
        _rumService.UserInfo = _userInfo;
      }
    }

    /// <summary>
    /// Gets or sets a custom application version identifier for all messages sent to Raygun.
    /// If this is not set, the package version will be used instead.
    /// </summary>
    public string ApplicationVersion
    {
      get { return _applicationVersion; }
      set
      {
        _applicationVersion = value;
        _rumService.ApplicationVersion = _applicationVersion;
      }
    }

    /// <summary>
    /// Raised just before any RaygunCrashReport is sent. This can be used to make final adjustments to the <see cref="RaygunCrashReport"/>, or to cancel the send.
    /// </summary>
    public event EventHandler<RaygunSendingCrashReportEventArgs> SendingCrashReport;

    /// <summary>
    /// Causes this RaygunClient to listen to and send all unhandled exceptions.
    /// </summary>
    /// <returns>The current RaygunClient instance.</returns>
    public RaygunClient EnableCrashReporting()
    {
      DisableCrashReporting();

      if (Application.Current != null)
      {
        Application.Current.UnhandledException += Application_UnhandledException;
      }

      SendStoredCrashReportsAsync();

      return Current;
    }

    /// <summary>
    /// Stops this RaygunClient from listening to unhandled exceptions.
    /// </summary>
    public void DisableCrashReporting()
    {
      if (Application.Current != null)
      {
        Application.Current.UnhandledException -= Application_UnhandledException;
      }
    }

    /// <summary>
    /// Causes this RaygunClient to listen to the application Resuming and Suspending events to automatically post session start/end events to Raygun.
    /// </summary>
    /// <returns>The current RaygunClient instance.</returns>
    public RaygunClient EnableRealUserMonitoring()
    {
      _rumService.Enable();

      return Current;
    }

    /// <summary>
    /// Stops this RaygunClient from listening to application Resuming and Suspending events.
    /// </summary>
    public void DisableRealUserMonitoring()
    {
      _rumService.Disable();
    }

    #region Manually send crash reports

    /// <summary>
    /// Asynchronously sends a crash report to Raygun for the given <see cref="Exception"/>.
    /// It is best to call this method within a try/catch block.
    /// If the application is crashing due to an unhandled exception, use the synchronous methods instead.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the crash report.</param>
    /// <param name="tags">An optional list of tags to send with the crash report.</param>
    /// <param name="userCustomData">Optional custom data to send with the crash report.</param>
    public async Task SendAsync(Exception exception, IList<string> tags = null, IDictionary userCustomData = null)
    {
      await StripAndSendAsync(exception, tags, userCustomData);
    }

    /// <summary>
    /// Asynchronously sends a RaygunCrashReport to Raygun.
    /// It is best to call this method within a try/catch block.
    /// If the application is crashing due to an unhandled exception, use the synchronous methods instead.
    /// </summary>
    /// <param name="raygunCrashReport">The RaygunCrashReport to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public async Task SendAsync(RaygunCrashReport raygunCrashReport)
    {
      await SendOrSaveCrashReportAsync(null, raygunCrashReport);
    }

    /// <summary>
    /// Sends a crash report immediately to Raygun for the given <see cref="Exception"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to send in the crash report.</param>
    /// <param name="tags">An optional list of tags to send with the crash report.</param>
    /// <param name="userCustomData">Optional custom data to send with the crash report.</param>
    public void Send(Exception exception, IList<string> tags = null, IDictionary userCustomData = null)
    {
      StripAndSend(exception, tags, userCustomData);
    }

    /// <summary>
    /// Sends a RaygunCrashReport immediately to Raygun.
    /// </summary>
    /// <param name="raygunCrashReport">The RaygunCrashReport to send. This needs its OccurredOn property
    /// set to a valid DateTime and as much of the Details property as is available.</param>
    public void Send(RaygunCrashReport raygunCrashReport)
    {
      SendOrSaveCrashReportAsync(null, raygunCrashReport).Wait(3000);
    }

    #endregion // Manually send crash reports

    #region Manually send RUM events

    /// <summary>
    /// Sends a RUM session start event which will have a newly generated session id.
    /// If there is already an active session, it will be ended before starting a new one.
    /// </summary>
    public async void SendSessionStartEventAsync()
    {
      await _rumService.SendSessionStartEventAsync();
    }

    /// <summary>
    /// Sends a RUM performance timing event.
    /// If there isn't currently an active session, a new one will be started.
    /// </summary>
    /// <param name="type">Type of event being recorded.</param>
    /// <param name="name">Name of the event (e.g. a page name or a request URL).</param>
    /// <param name="milliseconds">The duration of the event.</param>
    public async void SendSessionTimingEventAsync(RaygunRUMEventTimingType type, string name, long milliseconds)
    {
      await _rumService.SendSessionTimingEventAsync(type, name, milliseconds);
    }

    /// <summary>
    /// Sends a RUM session end event.
    /// This will do nothing if there isn't a currently active session.
    /// </summary>
    public async void SendSessionEndEventAsync()
    {
      await _rumService.SendSessionEndEventAsync();
    }

    #endregion // Manually send RUM events

    private void Application_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      Send(e.Exception);
    }

    private async Task SendOrSaveCrashReportAsync(Exception originalException, RaygunCrashReport raygunCrashReport)
    {
      if (ValidateApiKey())
      {
        bool canSend = OnSendingCrashReport(originalException, raygunCrashReport);
        if (canSend)
        {
          try
          {
            string payload = JsonConvert.SerializeObject(raygunCrashReport, HttpService.SERIALIZATION_SETTINGS);

            if (HttpService.IsInternetAvailable)
            {
              await SendCrashReportAsync(payload, true);
              SendStoredCrashReportsAsync();
            }
            else
            {
              await SaveCrashReportAsync(payload);
            }
          }
          catch (Exception ex)
          {
            Debug.WriteLine($"Error Logging Exception to Raygun: {ex.Message}");
          }
        }
      }
    }

    private bool ValidateApiKey()
    {
      if (string.IsNullOrEmpty(Settings.ApiKey))
      {
        Debug.WriteLine("ApiKey has not been provided, exception will not be logged");
        return false;
      }

      return true;
    }

    // Returns true if the crash report can be sent, false if the sending is canceled.
    private bool OnSendingCrashReport(Exception originalException, RaygunCrashReport raygunCrashReport)
    {
      bool result = true;

      if (!_handlingRecursiveErrorSending)
      {
        EventHandler<RaygunSendingCrashReportEventArgs> handler = SendingCrashReport;
        if (handler != null)
        {
          RaygunSendingCrashReportEventArgs args = new RaygunSendingCrashReportEventArgs(originalException, raygunCrashReport);
          try
          {
            handler(this, args);
          }
          catch (Exception e)
          {
            // Catch and send exceptions that occur in the SendingCrashReport event handler.
            // Set the _handlingRecursiveErrorSending flag to prevent infinite errors.
            _handlingRecursiveErrorSending = true;
            Send(e);
            _handlingRecursiveErrorSending = false;
          }

          result = !args.Cancel;
        }
      }

      return result;
    }

    private async void SendStoredCrashReportsAsync()
    {
      if (HttpService.IsInternetAvailable)
      {
        try
        {
          var tempFolder = ApplicationData.Current.TemporaryFolder;

          var raygunFolder = await tempFolder.CreateFolderAsync(OFFLINE_DATA_FOLDER, CreationCollisionOption.OpenIfExists).AsTask().ConfigureAwait(false);

          var files = await raygunFolder.GetFilesAsync().AsTask().ConfigureAwait(false);

          foreach (var file in files)
          {
            try
            {
              string text = await FileIO.ReadTextAsync(file).AsTask().ConfigureAwait(false);
              await SendCrashReportAsync(text, false);
            }
            catch (Exception ex)
            {
              Debug.WriteLine($"Failed to read stored crash report. The crash report will be deleted: {ex.Message}");
            }

            await file.DeleteAsync().AsTask().ConfigureAwait(false);
          }

          await raygunFolder.DeleteAsync().AsTask().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          Debug.WriteLine($"Error sending stored crash reports to Raygun: {ex.Message}");
        }
      }
    }

    private async Task SendCrashReportAsync(string payload, bool saveOnFail)
    {
      try
      {
        await HttpService.SendRequestAsync(Settings.CrashReportingApiEndpoint, Settings.ApiKey, payload);
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error Logging Exception to Raygun: {ex.Message}");
        if (saveOnFail)
        {
          SaveCrashReportAsync(payload).Wait(3000);
        }
      }
    }

    private static async Task SaveCrashReportAsync(string payload)
    {
      try
      {
        var tempFolder = ApplicationData.Current.TemporaryFolder;

        var raygunFolder = await tempFolder.CreateFolderAsync(OFFLINE_DATA_FOLDER, CreationCollisionOption.OpenIfExists).AsTask().ConfigureAwait(false);

        int number = 1;
        while (true)
        {
          bool exists;

          try
          {
            await raygunFolder.GetFileAsync($"RaygunCrashReport{number}.txt").AsTask().ConfigureAwait(false);
            exists = true;
          }
          catch (FileNotFoundException)
          {
            exists = false;
          }

          if (!exists)
          {
            string nextFileName = $"RaygunCrashReport{number + 1}.txt";

            try
            {
              StorageFile nextFile = await raygunFolder.GetFileAsync(nextFileName).AsTask().ConfigureAwait(false);

              await nextFile.DeleteAsync().AsTask().ConfigureAwait(false);
            }
            catch (FileNotFoundException)
            {
            }

            break;
          }

          number++;
        }

        if (number == 11)
        {
          try
          {
            StorageFile firstFile = await raygunFolder.GetFileAsync("RaygunCrashReport1.txt").AsTask().ConfigureAwait(false);
            await firstFile.DeleteAsync().AsTask().ConfigureAwait(false);
          }
          catch (FileNotFoundException)
          {
          }
        }

        string fileName = $"RaygunCrashReport{number}.txt";
        var file = await raygunFolder.CreateFileAsync(fileName).AsTask().ConfigureAwait(false);
        await FileIO.WriteTextAsync(file, payload).AsTask().ConfigureAwait(false);

        Debug.WriteLine($"Saved crash report: {OFFLINE_DATA_FOLDER}\\{fileName}");
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error saving crash report to offline storage: {ex.Message}");
      }
    }

    private RaygunCrashReport BuildCrashReport(Exception exception, IList<string> tags, IDictionary userCustomData, DateTime currentTime)
    {
      string version = string.IsNullOrWhiteSpace(ApplicationVersion) ? EnvironmentService.PackageVersion : ApplicationVersion;

      var crashReport = RaygunCrashReportBuilder.New
        .SetEnvironmentInfo()
        .SetOccurredOn(currentTime)
        .SetMachineName(new EasClientDeviceInformation().FriendlyName)
        .SetErrorInfo(exception)
        .SetClientInfo()
        .SetVersion(version)
        .SetTags(tags)
        .SetCustomData(userCustomData)
        .SetUserInfo(UserInfo ?? DefaultUserService.DefaultUser)
        .Build();

      return crashReport;
    }

    private void StripAndSend(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      var currentTime = DateTime.UtcNow;
      foreach (Exception e in StripWrapperExceptions(exception))
      {
        SendOrSaveCrashReportAsync(e, BuildCrashReport(e, tags, userCustomData, currentTime)).Wait(3000);
      }
    }

    private async Task StripAndSendAsync(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      var tasks = new List<Task>();
      var currentTime = DateTime.UtcNow;
      foreach (Exception e in StripWrapperExceptions(exception))
      {
        tasks.Add(SendOrSaveCrashReportAsync(e, BuildCrashReport(e, tags, userCustomData, currentTime)));
      }

      await Task.WhenAll(tasks);
    }

    private IEnumerable<Exception> StripWrapperExceptions(Exception exception)
    {
      if (exception != null && Settings.StrippedWrapperExceptions.Any(wrapperException => exception.GetType() == wrapperException && exception.InnerException != null))
      {
        AggregateException aggregate = exception as AggregateException;
        if (aggregate != null)
        {
          foreach (Exception e in aggregate.InnerExceptions)
          {
            foreach (Exception ex in StripWrapperExceptions(e))
            {
              yield return ex;
            }
          }
        }
        else
        {
          foreach (Exception e in StripWrapperExceptions(exception.InnerException))
          {
            yield return e;
          }
        }
      }
      else
      {
        yield return exception;
      }
    }
  }
}