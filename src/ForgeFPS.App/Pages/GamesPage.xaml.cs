using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ForgeFPS.Application;
using ForgeFPS.Domain;
using System.Linq;
using System.Threading.Tasks;

namespace ForgeFPS.App.Pages;

public sealed partial class GamesPage : Page
{
    private readonly DiagnosticService _diagnostic;

    public GamesPage()
    {
        InitializeComponent();
        _diagnostic = App.Services.GetRequiredService<DiagnosticService>();
        _ = LoadGamesAsync();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) => _ = LoadGamesAsync();

    private async Task LoadGamesAsync()
    {
        GamesPanel.Children.Clear();
        try
        {
            var games = await _diagnostic.DetectGamesAsync();
            if (games.Count == 0)
            {
                GamesPanel.Children.Add(new TextBlock
                {
                    Text = "Nenhum jogo detectado. Verifique se o Steam está instalado.",
                    FontSize = 14,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
                    Margin = new Thickness(24, 16, 0, 0)
                });
            }
            else
            {
                foreach (var game in games)
                {
                    var card = new Border
                    {
                        BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                        Padding = new Thickness(16),
                        Margin = new Thickness(0, 8, 0, 0)
                    };
                    var panel = new Microsoft.UI.Xaml.Controls.StackPanel { Spacing = 4 };
                    panel.Children.Add(new TextBlock { Text = game.Name, FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
                    panel.Children.Add(new TextBlock { Text = "Caminho: " + game.ExecutablePath, FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });
                    if (game.SteamAppId is not null)
                        panel.Children.Add(new TextBlock { Text = "Steam AppID: " + game.SteamAppId, FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });
                    card.Child = panel;
                    GamesPanel.Children.Add(card);
                }
            }
        }
        catch (Exception)
        {
        }
    }
}
