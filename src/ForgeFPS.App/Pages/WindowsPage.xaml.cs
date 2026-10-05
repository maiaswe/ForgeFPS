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
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ForgeFPS.App.Pages;

public sealed partial class WindowsPage : Page
{
    private readonly OptimizationCatalog _catalog;
    private readonly DiagnosticService _diagnostic;

    public WindowsPage()
    {
        InitializeComponent();
        _catalog = App.Services.GetRequiredService<OptimizationCatalog>();
        _diagnostic = App.Services.GetRequiredService<DiagnosticService>();
        Loaded += WindowsPage_Loaded;
    }

    private async void WindowsPage_Loaded(object sender, RoutedEventArgs e)
    {
        CardsPanel.Children.Clear();
        foreach (var action in _catalog.All.Where(a => a.Category != OptimizationCategory.GameSpecific))
        {
            var card = await BuildActionCardAsync(action);
            CardsPanel.Children.Add(card);
        }
    }

    private async Task<Border> BuildActionCardAsync(IOptimizationAction action)
    {
        var border = new Border
        {
            BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray),
            BorderThickness = new Thickness(1),
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12)
        };

        var panel = new StackPanel { Spacing = 6 };
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        titleRow.Children.Add(new TextBlock
        {
            Text = action.DisplayName,
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        titleRow.Children.Add(BuildRiskBadge(action.Risk));
        if (action.RequiresAdministrator)
        {
            titleRow.Children.Add(new Border
            {
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkOrange),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                Child = new TextBlock { Text = "Admin", FontSize = 11, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) }
            });
        }
        panel.Children.Add(titleRow);
        panel.Children.Add(new TextBlock { Text = action.Description, FontSize = 13, TextWrapping = TextWrapping.Wrap, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray) });
        if (action.ExpectedBenefit is not null)
            panel.Children.Add(new TextBlock { Text = "Benefício esperado: " + action.ExpectedBenefit, FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkSeaGreen) });

        var hardware = await _diagnostic.DetectHardwareAsync();
        var games = (await _diagnostic.DetectGamesAsync()).ToList();
        var detectionContext = new ApplyContext("DETECT", "", App.Services.GetRequiredService<ITransactionLog>(), (ForgeFPS.Domain.ILogger)new object(), App.Services.GetRequiredService<IFileSystem>(), App.Services.GetRequiredService<IRegistryRegistry>(), App.Services.GetRequiredService<IPowerPlanProvider>(), hardware, games, false);
        var state = await action.DetectCurrentStateAsync(detectionContext);
        foreach (var obs in state.Observations)
            panel.Children.Add(new TextBlock { Text = "• " + obs, FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightSteelBlue) });

        var buttonsRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };
        var simulateButton = new Button { Content = "Simular alterações" };
        simulateButton.Click += (s, args) => { _ = ShowSimulationAsync(action); };
        buttonsRow.Children.Add(simulateButton);

        var applyButton = new Button
        {
            Content = "Aplicar com segurança",
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.SteelBlue),
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
        };
        applyButton.Click += (s, args) => { _ = ApplyWithConsentAsync(action); };
        buttonsRow.Children.Add(applyButton);

        panel.Children.Add(buttonsRow);
        border.Child = panel;
        return border;
    }

    private static Border BuildRiskBadge(RiskLevel risk)
    {
        var (text, color) = risk switch
        {
            RiskLevel.Low => ("Baixo risco", Microsoft.UI.Colors.DarkGreen),
            RiskLevel.Moderate => ("Risco moderado", Microsoft.UI.Colors.DarkOrange),
            _ => ("Experimental", Microsoft.UI.Colors.DarkRed),
        };
        return new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(color),
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(4),
            Padding = new Thickness(6, 2, 6, 2),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = text, FontSize = 11, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) }
        };
    }

    private async Task ShowSimulationAsync(IOptimizationAction action)
    {
        var sessionService = App.Services.GetRequiredService<OptimizationSessionService>();
        var plans = await sessionService.SimulateAsync([action.Id]);
        var plan = plans.FirstOrDefault(p => p.ActionId == action.Id);
        var preview = plan?.Preview;

        var dialogContent = new StackPanel { Spacing = 8, Padding = new Thickness(0, 12, 0, 0) };
        dialogContent.Children.Add(new TextBlock { Text = "Prévia de alterações (modo simulação - nada será modificado):", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        foreach (var note in preview?.Notes ?? [])
            dialogContent.Children.Add(new TextBlock { Text = "• " + note, TextWrapping = TextWrapping.Wrap, FontSize = 12 });
        foreach (var warning in preview?.Warnings ?? [])
            dialogContent.Children.Add(new TextBlock { Text = "⚠ " + warning, TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange) });

        var dialog = new ContentDialog { Title = "Simular: " + action.DisplayName, Content = dialogContent, CloseButtonText = "OK", XamlRoot = Content.XamlRoot };
        await dialog.ShowAsync();
    }

    private async Task ApplyWithConsentAsync(IOptimizationAction action)
    {
        var sessionService = App.Services.GetRequiredService<OptimizationSessionService>();
        var plans = await sessionService.SimulateAsync([action.Id]);
        var plan = plans.FirstOrDefault(p => p.ActionId == action.Id);

        var consentContent = new StackPanel { Spacing = 8, Padding = new Thickness(0, 12, 0, 0) };
        consentContent.Children.Add(new TextBlock { Text = "As seguintes alterações serão aplicadas:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        foreach (var note in plan?.Preview?.Notes ?? [])
            consentContent.Children.Add(new TextBlock { Text = "• " + note, TextWrapping = TextWrapping.Wrap, FontSize = 12 });
        foreach (var warning in plan?.Preview?.Warnings ?? [])
            consentContent.Children.Add(new TextBlock { Text = "⚠ " + warning, TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange) });
        if (action.RequiresAdministrator)
            consentContent.Children.Add(new TextBlock { Text = "Esta ação requer privilégios de administrador.", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange) });
        consentContent.Children.Add(new TextBlock { Text = "Um backup será criado antes de qualquer alteração e você poderá reverter depois.", FontSize = 12 });

        var consentDialog = new ContentDialog { Title = "Confirmar: " + action.DisplayName, Content = consentContent, PrimaryButtonText = "Aplicar", CloseButtonText = "Cancelar", DefaultButton = ContentDialogButton.Close, XamlRoot = Content.XamlRoot };
        var result = await consentDialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        var outcome = await sessionService.ApplyWithConsentAsync([action.Id]);

        var resultContent = new StackPanel { Spacing = 8, Padding = new Thickness(0, 12, 0, 0) };
        resultContent.Children.Add(new TextBlock
        {
            Text = outcome.State switch
            {
                SessionState.Applied => "Alterações aplicadas e verificadas com sucesso.",
                SessionState.RolledBack => "A aplicação falhou e as alterações foram revertidas.",
                SessionState.RollbackFailed => "Falha na aplicação E na reversão. Verifique o histórico.",
                SessionState.Failed => "Falha na aplicação.",
                _ => "Estado desconhecido."
            },
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        foreach (var o in outcome.Outcomes)
            resultContent.Children.Add(new TextBlock { Text = "• " + o.ActionId + ": " + (o.VerificationResult?.Verified == true ? "verificado" : o.Success ? "aplicado" : "falhou"), FontSize = 12 });
        if (outcome.Summary is not null)
            resultContent.Children.Add(new TextBlock { Text = outcome.Summary, FontSize = 12 });

        var resultDialog = new ContentDialog { Title = "Resultado", Content = resultContent, CloseButtonText = "OK", XamlRoot = Content.XamlRoot };
        await resultDialog.ShowAsync();
    }
}
