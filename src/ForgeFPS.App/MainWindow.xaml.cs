using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ForgeFPS.App.Pages;

namespace ForgeFPS.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        NavView.SelectionChanged += NavView_SelectionChanged;
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            var pageType = tag switch
            {
                "home" => typeof(HomePage),
                "games" => typeof(GamesPage),
                "cs2" => typeof(Cs2Page),
                "windows" => typeof(WindowsPage),
                "benchmark" => typeof(BenchmarkPage),
                "history" => typeof(HistoryPage),
                "diagnostic" => typeof(DiagnosticPage),
                _ => null,
            };

            if (pageType is not null && NavFrame.CurrentSourcePageType != pageType)
            {
                NavFrame.Navigate(pageType);
            }
        }
    }
}