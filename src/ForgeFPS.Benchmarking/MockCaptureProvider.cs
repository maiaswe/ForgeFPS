namespace ForgeFPS.Benchmarking;

using Domain;

/// <summary>
/// Mock provider for testing — generates synthetic frame data.
/// </summary>
public class MockCaptureProvider(ILogger logger) : IPerformanceCaptureProvider
{
    public string ProviderName => "MockCaptureProvider";

    public Task<bool> IsAvailableAsync() => Task.FromResult(true);

    public async Task<BenchmarkMetrics> CaptureAsync(string processName, TimeSpan duration, ILogger logger)
    {
        logger.Information($"Mock capture started for {processName}, duration {duration.TotalSeconds}s.");
        await Task.Delay(duration);

        // Generate synthetic frametime data: normal distribution around 16.67ms (~60 FPS)
        var random = new Random(42); // fixed seed for reproducibility
        var samples = new List<FrameSample>();
        var frameTimes = new List<double>();
        var baseFps = 60.0;
        var frameTimeMs = 1000.0 / baseFps;

        var totalFrames = (int)(duration.TotalSeconds * baseFps);
        for (var i = 0; i < totalFrames; i++)
        {
            // Add some variance
            var ft = frameTimeMs + random.NextDouble() * 4 - 2; // ±2ms variance
            var ts = i * frameTimeMs;
            var isLate = ft > frameTimeMs * 1.1; // late if > 10% above target
            samples.Add(new FrameSample(i, ts, ft, ft * 0.8, ft * 0.6, isLate));
            frameTimes.Add(ft);
        }

        var context = new CaptureContext(processName, "TestGame", null, "1920x1080", "60", "test_map", DateTimeOffset.UtcNow);
        return PresentMonCsvParser.Parse(string.Empty, context) with
        {
            // Override with computed values
        };
    }
}