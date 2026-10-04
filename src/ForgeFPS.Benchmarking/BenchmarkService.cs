namespace ForgeFPS.Benchmarking;

using Domain;

/// <summary>
/// Interface for performance capture providers.
/// </summary>
public interface IPerformanceCaptureProvider
{
    string ProviderName { get; }
    Task<bool> IsAvailableAsync();
    Task<BenchmarkMetrics> CaptureAsync(string processName, TimeSpan duration, ILogger logger);
}

/// <summary>
/// Placeholder benchmark service.
/// </summary>
public class BenchmarkService(ILogger logger)
{
    public async Task<BenchmarkMetrics> RunBenchmarkAsync(
        IPerformanceCaptureProvider provider,
        string processName,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        logger.Information($"Benchmark started for {processName} with provider {provider.ProviderName}.");

        if (!await provider.IsAvailableAsync())
            throw new InvalidOperationException($"Provider {provider.ProviderName} is not available.");

        var metrics = await provider.CaptureAsync(processName, duration, logger);
        logger.Information($"Benchmark completed for {processName}. FPS: {metrics.AverageFps}.");

        return metrics;
    }

    public (double FpsImprovement, double FrameTimeImprovement)? CompareResults(
        BenchmarkMetrics before,
        BenchmarkMetrics after)
    {
        if (before.TotalFrames < 10 || after.TotalFrames < 10)
            return null;

        var fpsImprovement = ((after.AverageFps - before.AverageFps) / before.AverageFps) * 100;
        var frameTimeImprovement = ((after.AverageFrameTimeMs - before.AverageFrameTimeMs) / before.AverageFrameTimeMs) * 100;

        return (fpsImprovement, frameTimeImprovement);
    }
}