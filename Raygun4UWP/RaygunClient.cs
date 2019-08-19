using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.Web.Http;
using Newtonsoft.Json;

namespace Raygun4UWP
{
  public class RaygunClient
  {
    private const string OFFLINE_DATA_FOLDER = "Raygun4UWPOfflineCrashReports";

    private static RaygunClient _client;

    private readonly string _apiKey;
    private readonly List<Type> _wrapperExceptions = new List<Type>();
    private string _version;

    private bool _handlingRecursiveErrorSending;

    /// <summary>
    /// Initializes a new instance of the <see cref="RaygunClient" /> class.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    public RaygunClient(string apiKey)
    {
      _apiKey = apiKey;

      _wrapperExceptions.Add(typeof(TargetInvocationException));

      BeginSendStoredCrashReports();
    }

    private async void BeginSendStoredCrashReports()
    {
      await SendStoredCrashReports();
    }

    private bool ValidateApiKey()
    {
      if (string.IsNullOrEmpty(_apiKey))
      {
        System.Diagnostics.Debug.WriteLine("ApiKey has not been provided, exception will not be logged");
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

    /// <summary>
    /// Gets or sets the user identity string.
    /// </summary>
    public string User { get; set; }

    /// <summary>
    /// Gets or sets richer data about the currently logged-in user
    /// </summary>
    public RaygunUserInfo UserInfo { get; set; }

    /// <summary>
    /// Gets or sets a custom application version identifier for all crash reports sent to Raygun.
    /// </summary>
    public string ApplicationVersion { get; set; }

    /// <summary>
    /// Raised just before any RaygunCrashReport is sent. This can be used to make final adjustments to the <see cref="RaygunCrashReport"/>, or to cancel the send.
    /// </summary>
    public event EventHandler<RaygunSendingCrashReportEventArgs> SendingCrashReport;

    /// <summary>
    /// Adds a list of outer exceptions that will be stripped, leaving only the valuable inner exception.
    /// This can be used when a wrapper exception, e.g. TargetInvocationException,
    /// contains the actual exception as the InnerException. The message and stack trace of the inner exception will then
    /// be used by Raygun for grouping and display. TargetInvocationException is added for you,
    /// but if you have other wrapper exceptions that you want stripped you can pass them in here.
    /// </summary>
    /// <param name="wrapperExceptions">Exception types that you want removed and replaced with their inner exception.</param>
    public void AddWrapperExceptions(params Type[] wrapperExceptions)
    {
      foreach (Type wrapper in wrapperExceptions)
      {
        if (!_wrapperExceptions.Contains(wrapper))
        {
          _wrapperExceptions.Add(wrapper);
        }
      }
    }

    /// <summary>
    /// Specifies types of wrapper exceptions that Raygun should send rather than stripping out and sending the inner exception.
    /// This can be used to remove the default wrapper exception (TargetInvocationException).
    /// </summary>
    /// <param name="wrapperExceptions">Exception types that should no longer be stripped away.</param>
    public void RemoveWrapperExceptions(params Type[] wrapperExceptions)
    {
      foreach (Type wrapper in wrapperExceptions)
      {
        _wrapperExceptions.Remove(wrapper);
      }
    }

    /// <summary>
    /// Gets the <see cref="RaygunClient"/> created by the Initialize method.
    /// </summary>
    public static RaygunClient Current
    {
      get { return _client; }
      private set { _client = value; }
    }

    /// <summary>
    /// Creates a new RaygunClient with the given apikey.
    /// The RaygunClient is set on the Current property, and then returned.
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
    /// Causes Raygun to listen to and send all unhandled exceptions.
    /// </summary>
    public RaygunClient EnableCrashReporting()
    {
      DisableCrashReporting();

      if (Application.Current != null)
      {
        Application.Current.UnhandledException += Current_UnhandledException;
      }

      return Current;
    }

    /// <summary>
    /// Stops Raygun from listening to unhandled exceptions.
    /// </summary>
    public void DisableCrashReporting()
    {
      if (Application.Current != null)
      {
        Application.Current.UnhandledException -= Current_UnhandledException;
      }
    }

    private static void Current_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      _client.Send(e.Exception);
    }

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
      await SendOrSaveCrashReport(null, raygunCrashReport);
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
      SendOrSaveCrashReport(null, raygunCrashReport).Wait(3000);
    }

    private bool InternetAvailable()
    {
      IEnumerable<ConnectionProfile> connections = NetworkInformation.GetConnectionProfiles();
      var internetProfile = NetworkInformation.GetInternetConnectionProfile();

      bool internetAvailable = connections != null && connections.Any(c =>
        c.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess) ||
        (internetProfile != null && internetProfile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
      return internetAvailable;
    }

    private async Task SendOrSaveCrashReport(Exception originalException, RaygunCrashReport raygunCrashReport)
    {
      if (ValidateApiKey())
      {
        bool canSend = OnSendingCrashReport(originalException, raygunCrashReport);
        if (canSend)
        {
          try
          {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
              NullValueHandling = NullValueHandling.Ignore,
              Formatting = Formatting.None
            };

            string payload = JsonConvert.SerializeObject(raygunCrashReport, settings);

            if (InternetAvailable())
            {
              await SendCrashReport(payload);
            }
            else
            {
              await SaveCrashReport(payload);
            }
          }
          catch (Exception ex)
          {
            Debug.WriteLine($"Error Logging Exception to Raygun: {ex.Message}");
          }
        }
      }
    }

    private bool _saveOnFail = true;

    private async Task SendStoredCrashReports()
    {
      if (InternetAvailable())
      {
        _saveOnFail = false;
        try
        {
          var tempFolder = ApplicationData.Current.TemporaryFolder;

          var raygunFolder = await tempFolder.CreateFolderAsync(OFFLINE_DATA_FOLDER, CreationCollisionOption.OpenIfExists).AsTask().ConfigureAwait(false);

          var files = await raygunFolder.GetFilesAsync().AsTask().ConfigureAwait(false);

          foreach (var file in files)
          {
            string text = await FileIO.ReadTextAsync(file).AsTask().ConfigureAwait(false);
            await SendCrashReport(text).ConfigureAwait(false);

            await file.DeleteAsync().AsTask().ConfigureAwait(false);
          }

          await raygunFolder.DeleteAsync().AsTask().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
          Debug.WriteLine($"Error sending stored crash reports to Raygun: {ex.Message}");
        }
        finally
        {
          _saveOnFail = true;
        }
      }
    }

    private async Task SendCrashReport(string payload)
    {
      var httpClient = new HttpClient();

      var request = new HttpRequestMessage(HttpMethod.Post, RaygunSettings.Settings.CrashReportingApiEndpoint);
      request.Headers.Add("X-ApiKey", _apiKey);
      request.Content = new HttpStringContent(payload, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");

      try
      {
        await httpClient.SendRequestAsync(request, HttpCompletionOption.ResponseHeadersRead).AsTask().ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Error Logging Exception to Raygun: {ex.Message}");
        if (_saveOnFail)
        {
          SaveCrashReport(payload).Wait(3000);
        }
      }
    }

    private async Task SaveCrashReport(string payload)
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

            StorageFile nextFile = null;
            try
            {
              nextFile = await raygunFolder.GetFileAsync(nextFileName).AsTask().ConfigureAwait(false);

              await nextFile.DeleteAsync().AsTask().ConfigureAwait(false);
            }
            catch (FileNotFoundException) { }

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
          catch (FileNotFoundException) { }
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

    private RaygunCrashReport BuildCrashReport(Exception exception, IList<string> tags, IDictionary userCustomData, DateTime? currentTime)
    {
      string version = PackageVersion;
      if (!string.IsNullOrWhiteSpace(ApplicationVersion))
      {
        version = ApplicationVersion;
      }

      var crashReport = RaygunCrashReportBuilder.New
          .SetEnvironmentInfo()
          .SetOccurredOn(currentTime)
          .SetMachineName(new EasClientDeviceInformation().FriendlyName)
          .SetErrorInfo(exception)
          .SetClientInfo()
          .SetVersion(version)
          .SetTags(tags)
          .SetCustomData(userCustomData)
          .SetUserInfo(UserInfo ?? (!string.IsNullOrEmpty(User) ? new RaygunUserInfo(User) : null))
          .Build();

      return crashReport;
    }

    private string PackageVersion
    {
      get
      {
        if (_version == null)
        {
          try
          {
            // The try catch block is to get the tests to work.
            var v = Windows.ApplicationModel.Package.Current.Id.Version;
            _version = $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
          }
          catch (Exception) { }
        }

        return _version;
      }
    }

    private void StripAndSend(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      var currentTime = DateTime.UtcNow;
      foreach (Exception e in StripWrapperExceptions(exception))
      {
        SendOrSaveCrashReport(e, BuildCrashReport(e, tags, userCustomData, currentTime)).Wait(3000);
      }
    }

    private async Task StripAndSendAsync(Exception exception, IList<string> tags, IDictionary userCustomData)
    {
      var tasks = new List<Task>();
      var currentTime = DateTime.UtcNow;
      foreach (Exception e in StripWrapperExceptions(exception))
      {
        tasks.Add(SendOrSaveCrashReport(e, BuildCrashReport(e, tags, userCustomData, currentTime)));
      }
      await Task.WhenAll(tasks);
    }

    private IEnumerable<Exception> StripWrapperExceptions(Exception exception)
    {
      if (exception != null && _wrapperExceptions.Any(wrapperException => exception.GetType() == wrapperException && exception.InnerException != null))
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