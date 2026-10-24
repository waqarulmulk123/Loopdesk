using Microsoft.UI.Xaml;

namespace LiveWallpaperStudio.App;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Closed += (_, _) => Services.RenderSupervisor.Instance.Stop();
        _window.Activate();
    }
}
