using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using ForgeFPS.Application;
using ForgeFPS.Domain;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ForgeFPS.App;

public sealed partial class MainWindow : Window
{
    private readonly IServiceProvider _services;

    public MainWindow(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();
        NavigationView.SelectionChanged += NavigationView_SelectionChanged;
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            switch (tag)
            {
                case "Home":
                    ContentFrame.Navigate(typeof(HomePage));
                    break;
                case "Games":
                    ContentFrame.Navigate(typeof(GamesPage));
                    break;
                case "CS2":
                    ContentFrame.Navigate(typeof(Cs2Page));
                    break;
                case "Windows":
                    ContentFrame.Navigate(typeof(WindowsPage));
                    break;
                case "Benchmark":
                    ContentFrame.Navigate(typeof(BenchmarkPage));
                    break;
                case "History":
                    ContentFrame.Navigate(typeof(HistoryPage));
                    break;
                case "Diagnostic":
                    ContentFrame.Navigate(typeof(DiagnosticPage));
                    break;
            }
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ContentFrame.Navigate(typeof(HomePage));
    }
}

public class HomePage : Page
{
    private readonly DiagnosticService _diagnostic;
    private readonly OptimizationSessionService _sessionService;

    public HomePage()
    {
        _diagnostic = (App.Current as App)!.Services.GetRequiredService<DiagnosticService>();
        _sessionService = (App.Current as App)!.Services.GetRequiredService<OptimizationSessionService>();

        var root = new StackPanel { Spacing = 16, Margin = new Thickness(32, 24, 0, 0) };

        var title = new TextBlock
        {
            Text = "ForgeFPS",
            FontSize = 36,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold
        };
        root.Children.Add(title);

        var subtitle = new TextBlock
        {
            Text = "Otimizador de Games para Windows e Counter-Strike 2",
            FontSize = 16,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
        };
        root.Children.Add(subtitle);

        var desc = new TextBlock
        {
            Text = "O ForgeFPS analisa seu hardware, recomenda otimizações seguras, faz backup antes de qualquer alteração e permite reverter tudo se necessário.",
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 600,
            Margin = new Thickness(0, 16, 0, 0)
        };
        root.Children.Add(desc);

        var btnAnalyze = new Button
        {
            Content = "Analisar meu PC",
            FontSize = 16,
            Padding = new Thickness(32, 16, 32, 16),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 24, 0, 0)
        };
        btnAnalyze.Click += async (s, e) => await AnalyzeAsync();
        root.Children.Add(btnAnalyze);

        _statusText = new TextBlock
        {
            Text = "",
            FontSize = 13,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGreen),
            Margin = new Thickness(0, 8, 0, 0)
        };
        root.Children.Add(_statusText);

        _progress = new ProgressBar
        {
            IsIndeterminate = true,
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 8, 0, 0),
            Width = 300,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        root.Children.Add(_progress);

        Content = root;
    }

    private TextBlock _statusText = null!;
    private ProgressBar _progress = null!;

    private async Task AnalyzeAsync()
    {
        _statusText.Text = "Detectando hardware...";
        _progress.Visibility = Visibility.Visible;
        _progress.IsIndeterminate = true;

        try
        {
            var hardware = await _diagnostic.DetectHardwareAsync();
            var games = await _diagnostic.DetectGamesAsync();

            _statusText.Text = $"Hardware detectado: {hardware.Cpus.Count} CPU(s), {hardware.Gpus.Count} GPU(s), {hardware.TotalMemoryBytes / (1024 * 1024 * 1024)} GB RAM. Jogos: {games.Count}";
        }
        catch (Exception ex)
        {
            _statusText.Text = "Erro: " + ex.Message;
        }
        finally
        {
            _progress.Visibility = Visibility.Collapsed;
        }
    }
}

public class GamesPage : Page
{
    private readonly DiagnosticService _diagnostic;

    public GamesPage()
    {
        _diagnostic = (App.Current as App)!.Services.GetRequiredService<DiagnosticService>();

        var root = new StackPanel { Spacing = 12, Margin = new Thickness(32, 24, 0, 0) };

        var header = new TextBlock
        {
            Text = "Jogos Detectados",
            FontSize = 28,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold
        };
        root.Children.Add(header);

        var btnRefresh = new Button
        {
            Content = "Atualizar lista",
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 16)
        };
        btnRefresh.Click += async (s, e) => await LoadGamesAsync();
        root.Children.Add(btnRefresh);

        _gamesPanel = new StackPanel { Spacing = 8 };
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        scroll.Content = _gamesPanel;
        root.Children.Add(scroll);

        _statusText = new TextBlock
        {
            Text = "",
            FontSize = 13,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGreen),
            Margin = new Thickness(0, 8, 0, 0)
        };
        root.Children.Add(_statusText);

        Content = root;
        _ = LoadGamesAsync();
    }

    private StackPanel _gamesPanel = null!;
    private TextBlock _statusText = null!;

    private async Task LoadGamesAsync()
    {
        _statusText.Text = "Procurando jogos...";
        _gamesPanel.Children.Clear();

        try
        {
            var games = await _diagnostic.DetectGamesAsync();
            if (games.Count == 0)
            {
                _gamesPanel.Children.Add(new TextBlock
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
                        Margin = new Thickness(24, 8, 0, 0)
                    };
                    var panel = new StackPanel { Spacing = 4 };
                    panel.Children.Add(new TextBlock
                    {
                        Text = game.Name,
                        FontSize = 18,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    });
                    panel.Children.Add(new TextBlock
                    {
                        Text = "Caminho: " + game.ExecutablePath,
                        FontSize = 12,
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
                    });
                    if (game.SteamAppId is not null)
                        panel.Children.Add(new TextBlock
                        {
                            Text = "Steam AppID: " + game.SteamAppId,
                            FontSize = 12,
                            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
                        });
                    card.Child = panel;
                    _gamesPanel.Children.Add(card);
                }
                _statusText.Text = games.Count + " jogo(s) detectado(s).";
            }
        }
        catch (Exception ex)
        {
            _statusText.Text = "Erro: " + ex.Message;
        }
    }
}

public class Cs2Page : Page
{
    private readonly Cs2GameModule _cs2Module;
    private readonly DiagnosticService _diagnostic;

    public Cs2Page()
    {
        _cs2Module = (App.Current as App)!.Services.GetRequiredService<Cs2GameModule>();
        _diagnostic = (App.Current as App)!.Services.GetRequiredService<DiagnosticService>();

        var root = new StackPanel { Spacing = 12, Margin = new Thickness(32, 24, 0, 0) };

        var header = new TextBlock
        {
            Text = "Counter-Strike 2",
            FontSize = 28,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold
        };
        root.Children.Add(header);

        var btnDetect = new Button
        {
            Content = "Detectar CS2",
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 16)
        };
        btnDetect.Click += async (s, e) => await DetectAsync();
        root.Children.Add(btnDetect);

        _statusText = new TextBlock
        {
            FontSize = 13,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGreen),
            Margin = new Thickness(0, 8, 0, 0)
        };
        root.Children.Add(_statusText);

        _detailsPanel = new StackPanel { Spacing = 8 };
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        scroll.Content = _detailsPanel;
        root.Children.Add(scroll);

        Content = root;
        _ = DetectAsync();
    }

    private TextBlock _statusText = null!;
    private StackPanel _detailsPanel = null!;

    private async Task DetectAsync()
    {
        _statusText.Text = "Procurando CS2...";
        _detailsPanel.Children.Clear();

        try
        {
            var games = await _diagnostic.DetectGamesAsync();
            var cs2 = games.FirstOrDefault(g => g.SteamAppId == "730");

            if (cs2 is null)
            {
                _statusText.Text = "CS2 não detectado. Verifique se o Steam e o jogo estão instalados.";
                return;
            }

            _statusText.Text = "CS2 detectado: " + cs2.ExecutablePath;

            var card = new Border
            {
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray),
                BorderThickness = new Thickness(1),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 8, 0, 0)
            };
            var panel = new StackPanel { Spacing = 4 };
            panel.Children.Add(new TextBlock
            {
                Text = "Counter-Strike 2",
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });
            panel.Children.Add(new TextBlock { Text = "Caminho: " + cs2.ExecutablePath, FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });
            panel.Children.Add(new TextBlock { Text = "Biblioteca Steam: " + (cs2.LibraryPath ?? "desconhecida"), FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });
            panel.Children.Add(new TextBlock { Text = "AppID Steam: " + cs2.SteamAppId, FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });
            panel.Children.Add(new TextBlock { Text = "Modo online protegido (Trusted Mode): " + (cs2.IsOnlineProtected ? "Sim" : "Não"), FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray) });
            card.Child = panel;
            _detailsPanel.Children.Add(card);

            // Show available CS2 actions
            var actions = await _cs2Module.GetOptimizationActionsAsync(await _diagnostic.DetectHardwareAsync());
            foreach (var action in actions)
            {
                var actionCard = new Border
                {
                    BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DimGray),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 8, 0, 0)
                };
                var ap = new StackPanel { Spacing = 4 };
                ap.Children.Add(new TextBlock { Text = action.DisplayName, FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
                ap.Children.Add(new TextBlock { Text = action.Description, FontSize = 12, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray) });
                if (action.ExpectedBenefit is not null)
                    ap.Children.Add(new TextBlock { Text = "Benefício: " + action.ExpectedBenefit, FontSize = 11, FontStyle = Microsoft.UI.Text.FontStyle.Italic, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkSeaGreen) });
                actionCard.Child = ap;
                _detailsPanel.Children.Add(actionCard);
            }

            _statusText.Text = "CS2 detectado. " + actions.Count + " ação(ões) disponível(is).";
        }
        catch (Exception ex)
        {
            _statusText.Text = "Erro: " + ex.Message;
        }
    }
}

/// <summary>
/// Windows optimization page: lists all catalog actions as interactive cards.
/// </summary>
public class WindowsPage : Page
{
    private readonly OptimizationCatalog _catalog;
    private readonly StackPanel _cardsPanel = new() { Margin = new Thickness(24) };

    public WindowsPage()
    {
        _catalog = (App.Current as App)!.Services.GetRequiredService<OptimizationCatalog>();

        var rootPanel = new StackPanel { Spacing = 8 };
        var header = new TextBlock
        {
            Text = "Otimizações do Windows",
            FontSize = 28,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Margin = new Thickness(24, 24, 0, 8)
        };
        rootPanel.Children.Add(header);

        var subtitle = new TextBlock
        {
            Text = "Cada alteração é precedida de backup e totalmente reversível.",
            FontSize = 14,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
            Margin = new Thickness(24, 0, 0, 16)
        };
        rootPanel.Children.Add(subtitle);

        var scrollView = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        scrollView.Content = _cardsPanel;
        rootPanel.Children.Add(scrollView);

        Content = rootPanel;
        Loaded += WindowsPage_Loaded;
    }

    private async void WindowsPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _cardsPanel.Children.Clear();
        foreach (var action in _catalog.All)
        {
            var card = await BuildActionCardAsync(action);
            _cardsPanel.Children.Add(card);
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

        // Title row with risk badge
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

        panel.Children.Add(new TextBlock
        {
            Text = action.Description,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray)
        });

        if (action.ExpectedBenefit is not null)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Benefício esperado: " + action.ExpectedBenefit,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                FontStyle = Microsoft.UI.Text.FontStyle.Italic,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkSeaGreen)
            });
        }

        // Detect current state
        var diagnostic = (App.Current as App)!.Services.GetRequiredService<DiagnosticService>();
        var hardware = await diagnostic.DetectHardwareAsync();
        var games = await diagnostic.DetectGamesAsync();

        var gamesList = games.ToList();
        var detectionContext = new ApplyContext(
            sessionId: "DETECT",
            backupRoot: "",
            transactionLog: (App.Current as App)!.Services.GetRequiredService<ITransactionLog>(),
            logger: NullLogger.Instance,
            fileSystem: (App.Current as App)!.Services.GetRequiredService<IFileSystem>(),
            registry: (App.Current as App)!.Services.GetRequiredService<IRegistryRegistry>(),
            powerPlans: (App.Current as App)!.Services.GetRequiredService<IPowerPlanProvider>(),
            hardware: hardware,
            games: gamesList,
            isConsented: false);

        var state = await action.DetectCurrentStateAsync(detectionContext);
        foreach (var obs in state.Observations)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "• " + obs,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightSteelBlue)
            });
        }

        // Buttons row
        var buttonsRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };
        var simulateButton = new Button { Content = "Simular alterações" };
        simulateButton.Click += async (s, args) => { await ShowSimulationAsync(action); };
        buttonsRow.Children.Add(simulateButton);

        var applyButton = new Button
        {
            Content = "Aplicar com segurança",
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.SteelBlue),
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
        };
        applyButton.Click += async (s, args) => { await ApplyWithConsentAsync(action); };
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
        var diagnostic = (App.Current as App)!.Services.GetRequiredService<DiagnosticService>();
        var hardware = await diagnostic.DetectHardwareAsync();
        var games = (await diagnostic.DetectGamesAsync()).ToList();

        var simulationContext = new SimulationContext(
            NullLogger.Instance,
            (App.Current as App)!.Services.GetRequiredService<IFileSystem>(),
            (App.Current as App)!.Services.GetRequiredService<IRegistryRegistry>(),
            (App.Current as App)!.Services.GetRequiredService<IPowerPlanProvider>(),
            hardware,
            games);

        var preview = await action.PreviewAsync(simulationContext);

        var dialogContent = new StackPanel { Spacing = 8, Padding = new Thickness(0, 12, 0, 0) };
        dialogContent.Children.Add(new TextBlock
        {
            Text = "Prévia de alterações (modo simulação - nada será modificado):",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        foreach (var note in preview.Notes)
        {
            dialogContent.Children.Add(new TextBlock { Text = "• " + note, TextWrapping = TextWrapping.Wrap, FontSize = 12 });
        }
        foreach (var warning in preview.Warnings)
        {
            dialogContent.Children.Add(new TextBlock
            {
                Text = "⚠ " + warning,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange)
            });
        }

        var dialog = new ContentDialog
        {
            Title = "Simular: " + action.DisplayName,
            Content = dialogContent,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot
        };
        await dialog.ShowAsync();
    }

    /// <summary>
    /// Applies an action after explicit user consent, via the session service.
    /// </summary>
    private async Task ApplyWithConsentAsync(IOptimizationAction action)
    {
        var sessionService = (App.Current as App)!.Services.GetRequiredService<OptimizationSessionService>();

        // Show preview first
        var plans = await sessionService.SimulateAsync([action.Id]);
        var plan = plans.FirstOrDefault(p => p.ActionId == action.Id);

        var consentContent = new StackPanel { Spacing = 8, Padding = new Thickness(0, 12, 0, 0) };
        consentContent.Children.Add(new TextBlock
        {
            Text = "As seguintes alterações serão aplicadas:",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        foreach (var note in plan?.Preview?.Notes ?? [])
        {
            consentContent.Children.Add(new TextBlock { Text = "• " + note, TextWrapping = TextWrapping.Wrap, FontSize = 12 });
        }
        foreach (var warning in plan?.Preview?.Warnings ?? [])
        {
            consentContent.Children.Add(new TextBlock
            {
                Text = "⚠ " + warning,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange)
            });
        }
        if (action.RequiresAdministrator)
        {
            consentContent.Children.Add(new TextBlock
            {
                Text = "Esta ação requer privilégios de administrador.",
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange)
            });
        }
        consentContent.Children.Add(new TextBlock
        {
            Text = "Um backup será criado antes de qualquer alteração e você poderá reverter depois.",
            FontSize = 12,
            FontStyle = Microsoft.UI.Text.FontStyle.Italic
        });

        var consentDialog = new ContentDialog
        {
            Title = "Confirmar: " + action.DisplayName,
            Content = consentContent,
            PrimaryButtonText = "Aplicar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot
        };
        var result = await consentDialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        // Apply with consent
        var outcome = await sessionService.ApplyWithConsentAsync([action.Id]);

        var resultContent = new StackPanel { Spacing = 8, Padding = new Thickness(0, 12, 0, 0) };
        resultContent.Children.Add(new TextBlock
        {
            Text = outcome.State switch
            {
                SessionState.Applied => "✅ Alterações aplicadas e verificadas com sucesso.",
                SessionState.RolledBack => "⚠ A aplicação falhou e as alterações foram revertidas.",
                SessionState.RollbackFailed => "❌ Falha na aplicação E na reversão. Verifique o histórico.",
                _ => "❌ Falha na aplicação."
            },
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        foreach (var o in outcome.Outcomes)
        {
            resultContent.Children.Add(new TextBlock
            {
                Text = "• " + o.ActionId + ": " + (o.VerificationResult?.Verified == true ? "verificado" : o.Success ? "aplicado" : "falhou"),
                FontSize = 12
            });
        }
        if (outcome.Summary is not null)
        {
            resultContent.Children.Add(new TextBlock { Text = outcome.Summary, FontSize = 12, FontStyle = Microsoft.UI.Text.FontStyle.Italic });
        }

        var resultDialog = new ContentDialog
        {
            Title = "Resultado",
            Content = resultContent,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot
        };
        await resultDialog.ShowAsync();
    }
}

public class BenchmarkPage : Page
{
    private readonly BenchmarkRunner _runner;
    private readonly StackPanel _runsPanel = new() { Spacing = 8 };

    public BenchmarkPage()
    {
        _runner = (App.Current as App)!.Services.GetRequiredService<BenchmarkRunner>();

        var root = new StackPanel { Spacing = 16, Margin = new Thickness(32, 24, 0, 0) };

        var header = new TextBlock
        {
            Text = "Benchmark",
            FontSize = 28,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold
        };
        root.Children.Add(header);

        var subtitle = new TextBlock
        {
            Text = "Importe capturas do PresentMon (CSV) para comparar desempenho antes/depois.",
            FontSize = 14,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 600
        };
        root.Children.Add(subtitle);

        // Import section
        var importRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 16, 0, 8) };
        var btnImportBefore = new Button { Content = "Importar Antes", Padding = new Thickness(16, 8, 16, 8) };
        btnImportBefore.Click += async (s, e) => await ImportAsync(true);
        importRow.Children.Add(btnImportBefore);

        var btnImportAfter = new Button { Content = "Importar Depois", Padding = new Thickness(16, 8, 16, 8) };
        btnImportAfter.Click += async (s, e) => await ImportAsync(false);
        importRow.Children.Add(btnImportAfter);
        root.Children.Add(importRow);

        _statusText = new TextBlock
        {
            FontSize = 13,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGreen),
            Margin = new Thickness(0, 8, 0, 0)
        };
        root.Children.Add(_statusText);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        scroll.Content = _runsPanel;
        root.Children.Add(scroll);

        Content = root;
        RefreshRuns();
    }

    private TextBlock _statusText = null!;

    private void RefreshRuns()
    {
        _runsPanel.Children.Clear();
        foreach (var run in _runner.AllRuns)
        {
            var card = BuildRunCard(run);
            _runsPanel.Children.Add(card);
        }

        // Show comparison if we have before/after
        var before = _runner.AllRuns.FirstOrDefault(r => r.Title.StartsWith("Antes") || r.Title.StartsWith("Before"));
        var after = _runner.AllRuns.FirstOrDefault(r => r.Title.StartsWith("Depois") || r.Title.StartsWith("After"));
        if (before is not null && after is not null)
        {
            var comp = _runner.Compare(before, after);
            if (comp is not null)
            {
                _runsPanel.Children.Add(new Border
                {
                    BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 16, 0, 0),
                    Child = new StackPanel
                    {
                        Spacing = 8,
                        Children =
                        {
                            new TextBlock { Text = "Comparação Antes vs Depois", FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.Bold },
                            new TextBlock { Text = "FPS médio: " + comp.FpsImprovementPercent + "% (+" + (comp.FpsImprovementPercent > 0 ? "melhora" : "piora") + ")", FontSize = 14 },
                            new TextBlock { Text = "Frametime médio: " + comp.FrameTimeImprovementPercent + "% (" + (comp.FrameTimeImprovementPercent < 0 ? "melhora" : "piora") + ")", FontSize = 14 },
                            new TextBlock { Text = comp.IsStatisticallySignificant ? "✅ Significativo" : "⚠ Inconclusivo", FontSize = 14, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(comp.IsStatisticallySignificant ? Microsoft.UI.Colors.LightGreen : Microsoft.UI.Colors.Orange) },
                            new TextBlock { Text = comp.Verdict, FontSize = 13, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray) }
                        }
                    }
                });
            }
        }
    }

    private async Task ImportAsync(bool isBefore)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        picker.FileTypeFilter.Add(".csv");
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current as App);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var file = await picker.PickSingleFileAsync();
        if (file is null) return;

        try
        {
            _runner.AddFromCsv(file.Path, isBefore ? "Antes" : "Depois", "CS2", "Auto");
            _statusText.Text = (isBefore ? "Antes" : "Depois") + " importado: " + file.Name;
            RefreshRuns();
        }
        catch (Exception ex)
        {
            _statusText.Text = "Erro: " + ex.Message;
        }
    }
}

public class HistoryPage : Page
{
    private readonly IHistoryStore _historyStore;
    private readonly StackPanel _itemsPanel = new() { Margin = new Thickness(24) };

    public HistoryPage()
    {
        _historyStore = (App.Current as App)!.Services.GetRequiredService<IHistoryStore>();

        var rootPanel = new StackPanel();
        var header = new TextBlock
        {
            Text = "Histórico de sessões",
            FontSize = 28,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Margin = new Thickness(24, 24, 0, 8)
        };
        rootPanel.Children.Add(header);

        var subtitle = new TextBlock
        {
            Text = "Todas as sessões de otimização registradas nesta execução.",
            FontSize = 14,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray),
            Margin = new Thickness(24, 0, 0, 16)
        };
        rootPanel.Children.Add(subtitle);

        var scrollView = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        scrollView.Content = _itemsPanel;
        rootPanel.Children.Add(scrollView);

        Content = rootPanel;
        Loaded += HistoryPage_Loaded;
    }

    private void HistoryPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _itemsPanel.Children.Clear();
        var entries = _historyStore.GetAll();

        if (entries.Count == 0)
        {
            _itemsPanel.Children.Add(new TextBlock
            {
                Text = "Nenhuma sessão registrada ainda. Aplique uma otimização na página Windows.",
                FontSize = 14,
                FontStyle = Microsoft.UI.Text.FontStyle.Italic,
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
                Child = new TextBlock
                {
                    Text = stateText,
                    FontSize = 11,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
                }
            });
            panel.Children.Add(headerRow);

            panel.Children.Add(new TextBlock
            {
                Text = "Sessão: " + entry.SessionId[..Math.Min(16, entry.SessionId.Length)],
                FontSize = 12,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
            });

            foreach (var a in entry.Actions)
            {
                var detail = "• " + a.ActionId + ": " + (a.Verified == true ? "aplicada e verificada" : a.RollbackSuccess == true ? "revertida" : a.ApplySuccess ? "aplicada" : "falhou");
                panel.Children.Add(new TextBlock { Text = detail, FontSize = 12 });
            }

            if (entry.Summary is not null)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = entry.Summary,
                    FontSize = 12,
                    FontStyle = Microsoft.UI.Text.FontStyle.Italic,
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray)
                });
            }

            card.Child = panel;
            _itemsPanel.Children.Add(card);
        }
    }
}

public class DiagnosticPage : Page
{
    private readonly DiagnosticService _diagnostic;
    private readonly OptimizationCatalog _catalog;

    public DiagnosticPage()
    {
        _diagnostic = (App.Current as App)!.Services.GetRequiredService<DiagnosticService>();
        _catalog = (App.Current as App)!.Services.GetRequiredService<OptimizationCatalog>();

        var root = new StackPanel { Spacing = 12, Margin = new Thickness(32, 24, 0, 0) };

        var header = new TextBlock
        {
            Text = "Diagnóstico do Sistema",
            FontSize = 28,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold
        };
        root.Children.Add(header);

        var btnRun = new Button
        {
            Content = "Executar diagnóstico completo",
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 16)
        };
        btnRun.Click += async (s, e) => await RunDiagnosticAsync();
        root.Children.Add(btnRun);

        _statusText = new TextBlock
        {
            FontSize = 13,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGreen),
            Margin = new Thickness(0, 8, 0, 0)
        };
        root.Children.Add(_statusText);

        _detailsPanel = new StackPanel { Spacing = 8 };
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        scroll.Content = _detailsPanel;
        root.Children.Add(scroll);

        Content = root;
        _ = RunDiagnosticAsync();
    }

    private TextBlock _statusText = null!;
    private StackPanel _detailsPanel = null!;

    private async Task RunDiagnosticAsync()
    {
        _statusText.Text = "Executando diagnóstico...";
        _detailsPanel.Children.Clear();

        try
        {
            var hardware = await _diagnostic.DetectHardwareAsync();
            var games = await _diagnostic.DetectGamesAsync();

            // Hardware summary
            AddSection("Hardware", new[]
            {
                "OS: " + hardware.Os.Name + " " + hardware.Os.Version + " (" + hardware.Os.BuildNumber + ")",
                "CPU: " + hardware.Cpus.Count + " CPU(s), " + hardware.Cpus.Sum(c => c.LogicalCores) + " núcleos lógicos",
                "GPU: " + hardware.Gpus.Count + " GPU(s)",
                "RAM: " + (hardware.TotalMemoryBytes / (1024L * 1024 * 1024)) + " GB",
                "Monitor(es): " + hardware.Monitors.Count,
                "Bateria: " + (hardware.IsOnBattery ? "Sim" : "Não")
            });

            // Compatibility matrix
            var actions = _catalog.All;
            _detailsPanel.Children.Add(new TextBlock
            {
                Text = "Compatibilidade das Ações",
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Margin = new Thickness(0, 16, 0, 8)
            });

            foreach (var action in actions)
            {
                var context = new ApplyContext(
                    sessionId: "DIAG", backupRoot: "",
                    transactionLog: (App.Current as App)!.Services.GetRequiredService<ITransactionLog>(),
                    logger: NullLogger.Instance,
                    fileSystem: (App.Current as App)!.Services.GetRequiredService<IFileSystem>(),
                    registry: (App.Current as App)!.Services.GetRequiredService<IRegistryRegistry>(),
                    powerPlans: (App.Current as App)!.Services.GetRequiredService<IPowerPlanProvider>(),
                    hardware: hardware,
                    games: games.ToList(),
                    isConsented: false);

                var compat = await action.CheckCompatibilityAsync(context);
                var card = new Border
                {
                    BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        compat.IsCompatible ? Microsoft.UI.Colors.DarkGreen : Microsoft.UI.Colors.DarkRed),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 4, 0, 0)
                };
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
                panel.Children.Add(new TextBlock { Text = action.DisplayName, FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
                panel.Children.Add(new Border
                {
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        compat.IsCompatible ? Microsoft.UI.Colors.DarkGreen : Microsoft.UI.Colors.DarkRed),
                    CornerRadius = new Microsoft.UI.Xaml.CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2),
                    Child = new TextBlock { Text = compat.IsCompatible ? "Compatível" : "Incompatível", FontSize = 11, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White) }
                });
                if (compat.Notes.Count > 0)
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = string.Join("; ", compat.Notes),
                        FontSize = 11,
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray)
                    });
                }
                card.Child = panel;
                _detailsPanel.Children.Add(card);
            }

            _statusText.Text = "Diagnóstico concluído: " + actions.Count + " ações verificadas.";
        }
        catch (Exception ex)
        {
            _statusText.Text = "Erro: " + ex.Message;
        }
    }

    private void AddSection(string title, string[] items)
    {
        _detailsPanel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Margin = new Thickness(0, 16, 0, 8)
        });
        foreach (var item in items)
        {
            _detailsPanel.Children.Add(new TextBlock
            {
                Text = "• " + item,
                FontSize = 13,
                Margin = new Thickness(16, 2, 0, 2)
            });
        }
    }
}