using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.Networking.Connectivity;
using Windows.Web.Http;

namespace Raygun4UWP
{
  internal static class HttpService
  {
    private static HttpClient _httpClient;
    private static bool _isInternetAvailable;

    private static void Initialize()
    {
      if (_httpClient == null)
      {
        _httpClient = new HttpClient();

        NetworkInformation.NetworkStatusChanged += NetworkInformationOnNetworkStatusChanged;

        UpdateIsInternetAvailable();
      }
    }

    public static IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> SendRequestAsync(Uri endpoint, string apiKey, string payload)
    {
      Initialize();

      var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
      request.Headers.Add("X-ApiKey", apiKey);
      request.Content = new HttpStringContent(payload, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");

      return _httpClient.SendRequestAsync(request, HttpCompletionOption.ResponseHeadersRead);
    }

    public static bool IsInternetAvailable
    {
      get
      {
        Initialize();

        return _isInternetAvailable;
      }
    }

    private static void NetworkInformationOnNetworkStatusChanged(object sender)
    {
      UpdateIsInternetAvailable();
    }

    private static void UpdateIsInternetAvailable()
    {
      IEnumerable<ConnectionProfile> connections = NetworkInformation.GetConnectionProfiles();
      var internetProfile = NetworkInformation.GetInternetConnectionProfile();

      _isInternetAvailable = connections != null && connections.Any(c =>
                               c.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess) ||
                             (internetProfile != null && internetProfile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
    }
  }
}
