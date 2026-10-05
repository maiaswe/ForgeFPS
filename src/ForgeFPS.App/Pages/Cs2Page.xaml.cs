using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ForgeFPS.Application;
using ForgeFPS.Domain;
using ForgeFPS.Games.CS2;
using System.Linq;
using System.Threading.Tasks;

namespace ForgeFPS.App.Pages;

public sealed partial class Cs2Page : Page
{
    private readonly Cs2GameModule _cs2Module;
    private readonly DiagnosticService _diagnostic;

    public Cs2Page()
    {
        InitializeComponent();
        _cs2Module = App.Services.GetRequiredService<Cs2GameModule>();
        _diagnostic = App.Services.GetRequiredService<DiagnosticService>();
        _ = DetectAsync();
    }

    private void DetectButton_Click(object sender, RoutedEventArgs e) => _ = DetectAsync();

    private async Task DetectAsync()
    {
        DetailsPanel.Children.Clear();
        try
        {
            var games = await _diagnostic.DetectGamesAsync();
            var cs2 = games.FirstOrDefault(g => g.SteamAppId == "730");

            if (cs2 is null)
            {
                DetailsPanel.Children.Add(new TextBlock { Text = "CS2 não detectado.", FontSize = 14, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });
                return;
            }

            var card = new Border
            {
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray),
                BorderThickness = new Thickness(1),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 8, 0, 0)
            };
            var panel = new Microsoft.UI.Xaml.Controls.StackPanel { Spacing = 4 };
            panel.Children.Add(new TextBlock { Text = "Counter-Strike 2", FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            panel.Children.Add(new TextBlock { Text = "Caminho: " + cs2.ExecutablePath, FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });
            panel.Children.Add(new TextBlock { Text = "Biblioteca Steam: " + (cs2.LibraryPath ?? "desconhecida"), FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });
            panel.Children.Add(new TextBlock { Text = "AppID Steam: " + cs2.SteamAppId, FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });
            panel.Children.Add(new TextBlock { Text = "Trusted Mode: " + (cs2.IsOnlineProtected ? "Sim" : "Não"), FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });
            card.Child = panel;
            DetailsPanel.Children.Add(card);

            var actions = await _cs2Module.GetOptimizationActionsAsync(await _diagnostic.DetectHardwareAsync());
            foreach (var action in actions)
            {
                var actionCard = new Border { BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray), BorderThickness = new Thickness(1), CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8), Padding = new Thickness(12), Margin = new Thickness(0, 8, 0, 0) };
                var ap = new Microsoft.UI.Xaml.Controls.StackPanel { Spacing = 4 };
                ap.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = action.DisplayName, FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
                ap.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = action.Description, FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray) });
                if (action.ExpectedBenefit is not null)
                    ap.Children.Add(new Microsoft.UI.Xaml.Controls.TextBlock { Text = "Benefício: " + action.ExpectedBenefit, FontSize = 11, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkSeaGreen) });
                actionCard.Child = ap;
                DetailsPanel.Children.Add(actionCard);
            }
        }
        catch (Exception)
        {
        }
    }
}
