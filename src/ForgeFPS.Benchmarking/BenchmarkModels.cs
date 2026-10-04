namespace ForgeFPS.Benchmarking;

/// <summary>
/// Result of a before/after benchmark comparison.
/// </summary>
public record BenchmarkComparison(
    Domain.BenchmarkMetrics Before,
    Domain.BenchmarkMetrics After,
    double FpsImprovementPercent,
    double FrameTimeImprovementPercent,
    bool IsStatisticallySignificant,
    string Verdict);

/// <summary>
/// Describes where the benchmark was captured.
/// </summary>
public record CaptureContext(
    string ProcessName,
    string GameName,
    string? ProfileUsed,
    string Resolution,
    string RefreshRateHz,
    string MapOrScene,
    DateTimeOffset StartedAtUtc);