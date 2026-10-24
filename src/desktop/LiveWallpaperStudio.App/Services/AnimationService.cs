using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;

namespace LiveWallpaperStudio.App.Services;

/// <summary>
/// Animated UI helpers (WinUI Storyboards, no external animation
/// library). FadeIn is used on page load; Pulse runs forever on the
/// Home page's animated graphics banner and stops when the page is
/// unloaded, so it never animates in the background.
/// </summary>
public static class AnimationService
{
    public static Storyboard FadeIn(UIElement target, double from = 0, double to = 1, double seconds = 0.4)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromSeconds(seconds),
        };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, "Opacity");
        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        return storyboard;
    }

    public static Storyboard Pulse(UIElement target, double seconds = 2.5)
    {
        var animation = new DoubleAnimation
        {
            From = 0.55,
            To = 1.0,
            Duration = TimeSpan.FromSeconds(seconds),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
        };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, "Opacity");
        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        return storyboard;
    }
}
