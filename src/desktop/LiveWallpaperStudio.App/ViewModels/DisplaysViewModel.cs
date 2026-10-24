using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LiveWallpaperStudio.App.ViewModels;

public sealed partial class DisplaysViewModel : ObservableObject
{
    public DisplaysViewModel() => Refresh();

    [ObservableProperty]
    private string _displaySummary = "Displays: unknown";

    [RelayCommand]
    private void Refresh()
    {
        int monitors = GetSystemMetrics(80);
        int w = GetSystemMetrics(0);
        int h = GetSystemMetrics(1);
        DisplaySummary = monitors > 0
            ? $"Displays: {monitors} monitor(s) detected; primary {w}x{h}."
            : "Displays: could not read monitor count from Win32.";
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}
