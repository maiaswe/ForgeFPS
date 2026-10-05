using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


using Microsoft.UI.Xaml;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using ForgeFPS.Application;
using ForgeFPS.Domain;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.Win32;
using System.Linq;
using System.Threading.Tasks;

namespace ForgeFPS.App.Pages;

public sealed partial class DiagnosticPage : Page
{
    private readonly DiagnosticService _diagnostic;
    private readonly OptimizationCatalog _catalog;

    public DiagnosticPage()
    {
        InitializeComponent();
        _diagnostic = App.Services.GetRequiredService<DiagnosticService>();
        _catalog = App.Services.GetRequiredService<OptimizationCatalog>();
        Loaded += (_, _) => _ = RunDiagnosticAsync();
    }

    private void RunDiagnostic_Click(object sender, RoutedEventArgs e) => _ = RunDiagnosticAsync();

    private async Task RunDiagnosticAsync()
    {
        DetailsPanel.Children.Clear();
        try
        {
            var hardware = await _diagnostic.DetectHardwareAsync();
            var games = await _diagnostic.DetectGamesAsync();

            AddSection("Hardware", new[]
            {
                "OS: " + hardware.Os.Name + " " + hardware.Os.Version + " (" + hardware.Os.BuildNumber + ")",
                "CPU: " + hardware.Cpus.Count + " CPU(s), " + hardware.Cpus.Sum(c => c.LogicalCores) + " núcleos lógicos",
                "GPU: " + hardware.Gpus.Count + " GPU(s)",
                "RAM: " + (hardware.TotalMemoryBytes / (1024L * 1024 * 1024)) + " GB",
                "Monitor(es): " + hardware.Monitors.Count,
                "Bateria: " + (hardware.IsOnBattery ? "Sim" : "Não")
            });

            DetailsPanel.Children.Add(new TextBlock
            {
                Text = "Compatibilidade das Ações",
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Margin = new Thickness(0, 16, 0, 8)
            });

            foreach (var action in _catalog.All)
            {
                var context = new ApplyContext(
                    "DIAG", "",
                    App.Services.GetRequiredService<ITransactionLog>(),
                    (ForgeFPS.Domain.ILogger)new object(),
                    App.Services.GetRequiredService<IFileSystem>(),
                    App.Services.GetRequiredService<IRegistryRegistry>(),
                    App.Services.GetRequiredService<IPowerPlanProvider>(),
                    hardware, games.ToList(), false);

                var compat = await action.CheckCompatibilityAsync(context);
                var card = new Border
                {
                    BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(compat.IsCompatible ? Microsoft.UI.Colors.DarkGreen : Microsoft.UI.Colors.DarkRed),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 4, 0, 0)
                };
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                panel.Children.Add(new TextBlock { Text = action.DisplayName, FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
                panel.Children.Add(new Border
                {
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(compat.IsCompatible ? Microsoft.UI.Colors.DarkGreen : Microsoft.UI.Colors.DarkRed),
                    CornerRadius = new Microsoft.UI.Xaml.CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2),
                    Child = new TextBlock { Text = compat.IsCompatible ? "Compatível" : "Incompatível", FontSize = 11, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) }
                });
                if (compat.Notes.Count > 0)
                    panel.Children.Add(new TextBlock { Text = string.Join("; ", compat.Notes), FontSize = 11, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray) });
                card.Child = panel;
                DetailsPanel.Children.Add(card);
            }
        }
        catch
        {
        }
    }

    private void AddSection(string title, string[] items)
    {
        DetailsPanel.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Margin = new Thickness(0, 16, 0, 8) });
        foreach (var item in items)
            DetailsPanel.Children.Add(new TextBlock { Text = "• " + item, FontSize = 13, Margin = new Thickness(16, 2, 0, 2) });
    }
}
