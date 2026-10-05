using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using System.IO;
using ForgeFPS.Application;
using ForgeFPS.Infrastructure.Windows;
using ForgeFPS.Games.CS2;
using ForgeFPS.Benchmarking;
using ForgeFPS.Infrastructure.Windows.Actions;

namespace ForgeFPS.App;

public sealed partial class App : global::Microsoft.UI.Xaml.Application
{
    public static IServiceProvider Services { get; private set; }

    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
    }

    private IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
            builder.AddConsole().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information));

        services.AddSingleton<FileSystem>();
                services.AddSingleton<RegistryRegistry>();
                services.AddSingleton<PowerPlanProvider>();
                services.AddSingleton<InMemoryTransactionLog>();
                services.AddSingleton<PathValidator>();
                services.AddSingleton<AtomicWriter>();
                services.AddSingleton<SnapshotService>();
        
        services.AddSingleton<DetectCurrentPowerPlanAction>();
        services.AddSingleton<CreateGamingPowerPlanAction>();
        services.AddSingleton<GameModeAction>();
        services.AddSingleton<GpuPreferenceAction>();
        services.AddSingleton<Cs2ConfigApplyAction>();
        services.AddSingleton<Cs2LaunchOptionsAction>();

        services.AddSingleton<OptimizationCatalog>(sp =>
        {
            var catalog = new OptimizationCatalog();
            catalog.Register(sp.GetRequiredService<DetectCurrentPowerPlanAction>());
            catalog.Register(sp.GetRequiredService<CreateGamingPowerPlanAction>());
            catalog.Register(sp.GetRequiredService<GameModeAction>());
            catalog.Register(sp.GetRequiredService<GpuPreferenceAction>());
            catalog.Register(sp.GetRequiredService<Cs2ConfigApplyAction>());
            catalog.Register(sp.GetRequiredService<Cs2LaunchOptionsAction>());
            return catalog;
        });

        services.AddSingleton<OptimizationTransactionEngine>();
        services.AddSingleton<DiagnosticService>();
        services.AddSingleton<BenchmarkService>();
        services.AddSingleton<HistoryStore>();
                services.AddSingleton<OptimizationSessionService>();

        services.AddSingleton<Cs2GameModule>();
        
        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _ = new MainWindow();
    }
}
