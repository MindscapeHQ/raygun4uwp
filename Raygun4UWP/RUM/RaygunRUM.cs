using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Raygun4UWP
{
  public class RaygunRUM
  {
    public static readonly DependencyProperty ListenToNavigationProperty =
      DependencyProperty.RegisterAttached(
        "ListenToNavigation",
        typeof(bool),
        typeof(RaygunRUM),
        new PropertyMetadata(false)
      );

    public static void SetListenToNavigation(UIElement element, bool value)
    {
      element.SetValue(ListenToNavigationProperty, value);

      if (value)
      {
        if (element is Frame frame)
        {
          frame.Navigating += Frame_OnNavigating;
          frame.Navigated += Frame_OnNavigated;
        }
      }
    }

    private static void Frame_OnNavigating(object sender, NavigatingCancelEventArgs e)
    {

    }

    private static void Frame_OnNavigated(object sender, NavigationEventArgs e)
    {
      if (RaygunClient.Current != null && e.SourcePageType != null)
      {
        string name = e.SourcePageType.Name;
        RaygunClient.Current.SendSessionTimingEventAsync(RaygunRUMEventTimingType.ViewLoaded, name, 0);
      }
    }

    public static bool GetListenToNavigation(UIElement element)
    {
      return (bool) element.GetValue(ListenToNavigationProperty);
    }
  }
}
