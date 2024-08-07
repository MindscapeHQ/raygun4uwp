﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Raygun4UWP
{
  internal static class HttpService
  {
    public static readonly JsonSerializerSettings SERIALIZATION_SETTINGS;

    private static HttpClient _httpClient;
    private static bool _isInternetAvailable;

    static HttpService()
    {
      SERIALIZATION_SETTINGS = new JsonSerializerSettings
      {
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.None,
        Converters = new JsonConverter[] { new StringEnumConverter() }
      };
    }

    private static void Initialize()
    {
      if (_httpClient == null)
      {
        _httpClient = new HttpClient();

        NetworkInformation.NetworkStatusChanged += NetworkInformationOnNetworkStatusChanged;

        UpdateIsInternetAvailable();
      }
    }

    public static async Task SendRequestAsync(Uri endpoint, string apiKey, string payload)
    {
      Initialize();

      var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
      request.Headers.Add("X-ApiKey", apiKey);
      request.Content = new HttpStringContent(payload, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");

      await _httpClient.SendRequestAsync(request, HttpCompletionOption.ResponseHeadersRead).AsTask().ConfigureAwait(false);
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
      try
      {
        IEnumerable<ConnectionProfile> connections = NetworkInformation.GetConnectionProfiles();
        var internetProfile = NetworkInformation.GetInternetConnectionProfile();

        _isInternetAvailable = connections != null && connections.Any(c =>
                                 c.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess) ||
                               (internetProfile != null && internetProfile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
      }
      catch (Exception)
      {
        // If we can't determine the internet status, assume it's not available
        _isInternetAvailable = false;
      }
    }
  }
}
