using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using ForgeFPS.Application;
using ForgeFPS.Infrastructure.Windows;
using ForgeFPS.Games.CS2;
using ForgeFPS.Games.Abstractions;

namespace ForgeFPS.App;

public partial class App : Application
{
    public IServiceProvider Services { get; private set!; }

    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
    }

    private IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
            builder.AddConsole().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information));

        // Infrastructure
        services.AddSingleton<FileSystem>();
        services.AddSingleton<IFileSystem>(sp => sp.GetRequiredService<FileSystem>());
        services.AddSingleton<RegistryRegistry>();
        services.AddSingleton<IRegistryRegistry>(sp => sp.GetRequiredService<RegistryRegistry>());
        services.AddSingleton<PowerPlanProvider>();
        services.AddSingleton<IPowerPlanProvider>(sp => sp.GetRequiredService<PowerPlanProvider>());
        services.AddSingleton<InMemoryTransactionLog>();
        services.AddSingleton<ITransactionLog>(sp => sp.GetRequiredService<InMemoryTransactionLog>());
        services.AddSingleton<PathValidator>();
        services.AddSingleton<IPathValidator>(sp => sp.GetRequiredService<PathValidator>());
        services.AddSingleton<AtomicWriter>();
        services.AddSingleton<IAtomicWriter>(sp => sp.GetRequiredService<AtomicWriter>());
        services.AddSingleton<SnapshotService>();
        services.AddSingleton<ISnapshotService>(sp => sp.GetRequiredService<SnapshotService>());

        // Actions (register all as IOptimizationAction)
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

        // Application services
        services.AddSingleton<OptimizationTransactionEngine>();
        services.AddSingleton<DiagnosticService>();
        services.AddSingleton<BenchmarkService>();
        services.AddSingleton<HistoryStore>();
        services.AddSingleton<IHistoryStore>(sp => sp.GetRequiredService<HistoryStore>());
        services.AddSingleton<OptimizationSessionService>();

        // Games
        services.AddSingleton<Cs2GameModule>();
        services.AddSingleton<IGameModule>(sp => sp.GetRequiredService<Cs2GameModule>());

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _ = new MainWindow(Services).Show();
    }
}