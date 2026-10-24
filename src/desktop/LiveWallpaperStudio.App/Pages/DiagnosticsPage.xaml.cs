using LiveWallpaperStudio.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace LiveWallpaperStudio.App.Pages;

public sealed partial class DiagnosticsPage : Page
{
    public DiagnosticsViewModel ViewModel { get; } = new();

    public DiagnosticsPage()
    {
        InitializeComponent();

    }
}
