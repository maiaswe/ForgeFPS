using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


using Microsoft.UI.Xaml;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using ForgeFPS.Application;
using ForgeFPS.Domain;
using System.Linq;

namespace ForgeFPS.App.Pages;

public sealed partial class HistoryPage : Page
{
    private readonly IHistoryStore _historyStore;

    public HistoryPage()
    {
        InitializeComponent();
        _historyStore = App.Services.GetRequiredService<IHistoryStore>();
        Loaded += HistoryPage_Loaded;
    }

    private void HistoryPage_Loaded(object sender, RoutedEventArgs e)
    {
        ItemsPanel.Children.Clear();
        var entries = _historyStore.GetAll();

        if (entries.Count == 0)
        {
            ItemsPanel.Children.Add(new TextBlock
            {
                Text = "Nenhuma sessão registrada ainda. Aplique uma otimização na página Windows.",
                FontSize = 14,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
            });
            return;
        }

        foreach (var entry in entries)
        {
            var (stateText, stateColor) = entry.State switch
            {
                SessionState.Applied => ("Aplicada", Microsoft.UI.Colors.DarkGreen),
                SessionState.RolledBack => ("Revertida", Microsoft.UI.Colors.DarkOrange),
                SessionState.RollbackFailed => ("Falha na reversão", Microsoft.UI.Colors.DarkRed),
                SessionState.Failed => ("Falhou", Microsoft.UI.Colors.DarkRed),
                _ => (entry.State.ToString(), Microsoft.UI.Colors.Gray),
            };

            var card = new Border
            {
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray),
                BorderThickness = new Thickness(1),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 12)
            };
            var panel = new StackPanel { Spacing = 4 };
            var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            headerRow.Children.Add(new TextBlock
            {
                Text = entry.CompletedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            headerRow.Children.Add(new Border
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(stateColor),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = stateText, FontSize = 11, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) }
            });
            panel.Children.Add(headerRow);
            panel.Children.Add(new TextBlock
            {
                Text = "Sessão: " + entry.SessionId[..System.Math.Min(16, entry.SessionId.Length)],
                FontSize = 12,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
            });
            foreach (var a in entry.Actions)
            {
                var detail = "• " + a.ActionId + ": " + (a.Verified == true ? "aplicada e verificada" : a.RollbackSuccess == true ? "revertida" : a.ApplySuccess ? "aplicada" : "falhou");
                panel.Children.Add(new TextBlock { Text = detail, FontSize = 12 });
            }
            if (entry.Summary is not null)
                panel.Children.Add(new TextBlock { Text = entry.Summary, FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray) });

            card.Child = panel;
            ItemsPanel.Children.Add(card);
        }
    }
}
