using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ForgeFPS.Benchmarking;
using System;
using System.Linq;

namespace ForgeFPS.App.Pages;

public sealed partial class BenchmarkPage : Page
{
    private readonly BenchmarkRunner _runner;

    public BenchmarkPage()
    {
        InitializeComponent();
        _runner = App.Services.GetRequiredService<BenchmarkRunner>();
        RefreshRuns();
    }

    private void RefreshRuns()
    {
        RunsPanel.Children.Clear();
        foreach (var run in _runner.AllRuns)
        {
            var card = new Border
            {
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray),
                BorderThickness = new Thickness(1),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 8, 0, 0)
            };
            var panel = new StackPanel { Spacing = 4 };
            panel.Children.Add(new TextBlock { Text = run.Title, FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            panel.Children.Add(new TextBlock { Text = "FPS médio: " + run.Metrics.AverageFps + " | 1% low: " + run.Metrics.OnePercentLowFps + " | Frames: " + run.Metrics.TotalFrames, FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });
            card.Child = panel;
            RunsPanel.Children.Add(card);
        }

        var before = _runner.AllRuns.FirstOrDefault(r => r.Title.StartsWith("Antes") || r.Title.StartsWith("Before"));
        var after = _runner.AllRuns.FirstOrDefault(r => r.Title.StartsWith("Depois") || r.Title.StartsWith("After"));
        if (before is not null && after is not null)
        {
            var comp = _runner.Compare(before, after);
            if (comp is not null)
            {
                var comparisonCard = new Border
                {
                    BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 16, 0, 0)
                };
                var panel = new StackPanel { Spacing = 8 };
                panel.Children.Add(new TextBlock { Text = "Comparação Antes vs Depois", FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
                panel.Children.Add(new TextBlock { Text = "FPS médio: " + comp.FpsImprovementPercent + "% (" + (comp.FpsImprovementPercent > 0 ? "melhora" : "piora") + ")", FontSize = 14 });
                panel.Children.Add(new TextBlock { Text = "Frametime médio: " + comp.FrameTimeImprovementPercent + "% (" + (comp.FrameTimeImprovementPercent < 0 ? "melhora" : "piora") + ")", FontSize = 14 });
                panel.Children.Add(new TextBlock { Text = comp.IsStatisticallySignificant ? "✅ Significativo" : "⚠ Inconclusivo", FontSize = 14, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(comp.IsStatisticallySignificant ? Microsoft.UI.Colors.LightGreen : Microsoft.UI.Colors.Orange) });
                panel.Children.Add(new TextBlock { Text = comp.Verdict, FontSize = 13, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray) });
                comparisonCard.Child = panel;
                RunsPanel.Children.Add(comparisonCard);
            }
        }
    }

    private void ImportBefore_Click(object sender, RoutedEventArgs e) => _ = ImportAsync(true);
    private void ImportAfter_Click(object sender, RoutedEventArgs e) => _ = ImportAsync(false);

    private async Task ImportAsync(bool isBefore)
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".csv");
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current as App);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSingleFileAsync();
            if (file is null) return;

            _runner.AddFromCsv(file.Path, isBefore ? "Antes" : "Depois", "CS2", "Auto");
            RefreshRuns();
        }
        catch (Exception)
        {
        }
    }
}
