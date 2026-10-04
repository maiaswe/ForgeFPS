namespace ForgeFPS.Benchmarking;

using Domain;
using System.IO;

/// <summary>
/// In-memory benchmark runner that stores runs for later comparison.
/// </summary>
public class BenchmarkRunner(ILogger logger)
{
    private readonly List<BenchmarkRun> _runs = new();

    public IReadOnlyList<BenchmarkRun> AllRuns => _runs;

    public BenchmarkRun AddFromCsv(string csvPath, string title, string gameName, string? profileUsed)
    {
        if (!File.Exists(csvPath))
            throw new ArgumentException($"File not found: {csvPath}", nameof(csvPath));

        var content = File.ReadAllText(csvPath);
        var context = new CaptureContext(
            Path.GetFileNameWithoutExtension(csvPath),
            gameName,
            profileUsed,
            "unknown", "unknown", "unknown",
            DateTimeOffset.UtcNow);

        var metrics = PresentMonCsvParser.Parse(content, context);
        var run = new BenchmarkRun(
            Guid.NewGuid().ToString("N"),
            title,
            gameName,
            profileUsed,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            metrics,
            new[] { "Arquivo: " + csvPath });

        _runs.Add(run);
        logger.Information("Benchmark run added: " + run.Id + " — " + metrics.TotalFrames + " frames.");
        return run;
    }

    public BenchmarkComparison? Compare(BenchmarkRun before, BenchmarkRun after)
    {
        if (before.Metrics.TotalFrames < 10 || after.Metrics.TotalFrames < 10)
            return null;
        if (before.GameName != after.GameName)
            return null;

        var fpsDiff = ((after.Metrics.AverageFps - before.Metrics.AverageFps) / before.Metrics.AverageFps) * 100;
        var ftDiff = ((after.Metrics.AverageFrameTimeMs - before.Metrics.AverageFrameTimeMs) / before.Metrics.AverageFrameTimeMs) * 100;

        var isSignificant = Math.Abs(fpsDiff) > 2.0 || Math.Abs(ftDiff) > 2.0;

        var verdict = (fpsDiff > 2.0, ftDiff < -2.0) switch
        {
            (true, _) => "Melhora significativa de FPS.",
            (_, true) => "Melhora significativa de consistência de frametime.",
            (false, false) => "Variação dentro da margem de erro — resultado inconclusivo.",
        };

        return new BenchmarkComparison(
            before.Metrics,
            after.Metrics,
            Math.Round(fpsDiff, 2),
            Math.Round(ftDiff, 2),
            isSignificant,
            verdict);
    }
}