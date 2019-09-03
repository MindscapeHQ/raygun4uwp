using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Raygun4UWP
{
  public class RaygunRUM
  {
    private static readonly Stopwatch _stopwatch = new Stopwatch();

    public static readonly DependencyProperty ListenToNavigationProperty =
      DependencyProperty.RegisterAttached(
        "ListenToNavigation",
        typeof(bool),
        typeof(RaygunRUM),
        new PropertyMetadata(false)
      );

    /// <summary>
    /// Causes RaygunClient.Current to listen to navigation events of the given Frame element
    /// and send page navigation events to Raygun.
    /// </summary>
    /// <param name="element">A Frame element.</param>
    /// <param name="listenToNavigation">Whether or not to listen to navigation events.</param>
    public static void SetListenToNavigation(UIElement element, bool listenToNavigation)
    {
      element.SetValue(ListenToNavigationProperty, listenToNavigation);

      if (element is Frame frame)
      {
        if (listenToNavigation)
        {
          frame.Navigating += Frame_OnNavigating;
          frame.Navigated += Frame_OnNavigated;
        }
        else
        {
          frame.Navigating -= Frame_OnNavigating;
          frame.Navigated -= Frame_OnNavigated;
        }
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

    public static bool GetListenToNavigation(UIElement element)
    {
      return (bool) element.GetValue(ListenToNavigationProperty);
    }
  }
}
