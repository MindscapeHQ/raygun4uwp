using System;
using Windows.Foundation;
using Windows.Web.Http;

namespace Raygun4UWP
{
  internal static class HttpService
  {
    private static HttpClient _httpClient;

    private static void Initialize()
    {
      if (_httpClient == null)
      {
        _httpClient = new HttpClient();
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
  }
}
