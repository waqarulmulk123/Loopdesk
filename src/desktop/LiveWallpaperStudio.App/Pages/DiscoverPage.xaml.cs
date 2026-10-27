using LiveWallpaperStudio.App.ViewModels;
using System.ComponentModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LiveWallpaperStudio.App.Pages;

public sealed partial class DiscoverPage : Page
{
    public DiscoverViewModel ViewModel { get; } = new();

    public DiscoverPage()
    {
        InitializeComponent();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ViewModel.Categories.CollectionChanged += Categories_CollectionChanged;
        RebuildCategoryButtons();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DiscoverViewModel.SelectedCategory)) RebuildCategoryButtons();
    }

    private void Categories_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildCategoryButtons();

    private void RebuildCategoryButtons()
    {
        CategoryButtons.Children.Clear();
        foreach (string category in ViewModel.Categories)
        {
            CategoryButtons.Children.Add(new Button
            {
                Content = category,
                Padding = new Thickness(16, 7, 16, 7),
                Command = ViewModel.SelectCategoryCommand,
                CommandParameter = category,
                Style = Application.Current.Resources[category == ViewModel.SelectedCategory
                    ? "HomePrimaryButtonStyle" : "HomeSecondaryButtonStyle"] as Style,
            });
        }
    }

    private void CardAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DiscoverItem item }) ViewModel.SelectedItem = item;
    }
}
