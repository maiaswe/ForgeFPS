namespace ForgeFPS.Benchmarking;

using Domain;
using System.Globalization;

/// <summary>
/// CSV parser for PresentMon output. Handles different PresentMon versions
/// and culture-specific decimal separators (comma vs dot).
/// </summary>
/// <remarks>
/// PresentMon CSV columns (v3+):
///   Frame#,Timestamp_ms,FrameTime_ms,CPU_Time_ms,GPU_Time_ms,LateFrame
/// The parser auto-detects the decimal separator and column layout.
/// </remarks>
public static class PresentMonCsvParser
{
    private const string ColumnFrameTime = "FrameTime_ms";
    private const string ColumnTimestamp = "Timestamp_ms";
    private const string ColumnCpuTime = "CPU_Time_ms";
    private const string ColumnGpuTime = "GPU_Time_ms";
    private const string ColumnLateFrame = "LateFrame";
    private const string ColumnFrameIndex = "Frame#";

    /// <summary>
    /// Parses a PresentMon CSV string into frame samples and metrics.
    /// </summary>
    public static BenchmarkMetrics Parse(string csvContent, CaptureContext context)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
            throw new ArgumentException("CSV content is empty.", nameof(csvContent));

        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
            throw new ArgumentException("CSV must contain at least a header and one data row.", nameof(csvContent));

        // Detect decimal separator from first data row
        var decSep = DetectDecimalSeparator(lines[1]);
        var fmt = decSep == ',' ? CultureInfo.GetCultureInfo("pt-BR") : CultureInfo.InvariantCulture;

        // Parse header
        var headerLine = lines[0].Trim();
        var columns = headerLine.Split(',').Select(c => c.Trim('\"')).ToArray();
        var colIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < columns.Length; i++)
            colIndex[columns[i]] = i;

        // Required columns
        if (!colIndex.TryGetValue(ColumnFrameTime, out var ftIdx))
            throw new FormatException($"Missing required column: {ColumnFrameTime}");
        if (!colIndex.TryGetValue(ColumnTimestamp, out var tsIdx))
            throw new FormatException($"Missing required column: {ColumnTimestamp}");

        var frameTimes = new List<double>();
        var timestamps = new List<double>();
        var samples = new List<FrameSample>();
        double? targetFrameTime = null;

        if (context.RefreshRateHz != null && double.TryParse(context.RefreshRateHz, NumberStyles.Float, fmt, out var refreshRate))
            targetFrameTime = 1000.0 / refreshRate;

        for (var lineIdx = 1; lineIdx < lines.Length; lineIdx++)
        {
            var line = lines[lineIdx].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var fields = SplitCsvLine(line);
            if (fields.Count <= Math.Max(ftIdx, tsIdx)) continue;

            if (!double.TryParse(fields[ftIdx], NumberStyles.Float, fmt, out var frameTimeMs)) continue;
            if (!double.TryParse(fields[tsIdx], NumberStyles.Float, fmt, out var timestampMs)) continue;

            var cpuTime = fields.Count > colIndex.GetValueOrDefault(ColumnCpuTime, -1)
                && double.TryParse(fields[colIndex.GetValueOrDefault(ColumnCpuTime, -1)], NumberStyles.Float, fmt, out var ct)
                ? ct : 0.0;
            var gpuTime = fields.Count > colIndex.GetValueOrDefault(ColumnGpuTime, -1)
                && double.TryParse(fields[colIndex.GetValueOrDefault(ColumnGpuTime, -1)], NumberStyles.Float, fmt, out var gt)
                ? gt : 0.0;
            var isLate = fields.Count > colIndex.GetValueOrDefault(ColumnLateFrame, -1)
                && bool.TryParse(fields[colIndex.GetValueOrDefault(ColumnLateFrame, -1)], out var lf)
                ? lf : false;

            frameTimes.Add(frameTimeMs);
            timestamps.Add(timestampMs);
            samples.Add(new FrameSample(lineIdx - 1, timestampMs, frameTimeMs, cpuTime, gpuTime, isLate));
        }

        if (frameTimes.Count < 2)
            throw new InvalidOperationException($"Insufficient frames parsed: got {frameTimes.Count}, need at least 2.");

        return ComputeMetrics(samples, frameTimes, timestamps, context, targetFrameTime ?? 16.67);
    }

    public static BenchmarkMetrics ParseFile(string filePath, CaptureContext context)
    {
        if (!System.IO.File.Exists(filePath))
            throw new ArgumentException($"File not found: {filePath}", nameof(filePath));
        var content = System.IO.File.ReadAllText(filePath);
        return Parse(content, context);
    }

    private static BenchmarkMetrics ComputeMetrics(
        IReadOnlyList<FrameSample> samples,
        IReadOnlyList<double> frameTimes,
        IReadOnlyList<double> timestamps,
        CaptureContext context,
        double targetFrameTimeMs)
    {
        var totalFrames = frameTimes.Count;
        var durationMs = timestamps[^1] - timestamps[0];
        var avgFps = durationMs > 0 ? (totalFrames / durationMs) * 1000.0 : 0;
        var avgFrameTime = frameTimes.Average();

        // Percentile calculations — frametimes sorted ascending;
        // 1% low FPS = 1000 / (99th percentile frametime)
        // 0.1% low FPS = 1000 / (99.9th percentile frametime)
        var sorted = new List<double>(frameTimes.OrderBy(f => f));
        var p99Idx = (int)Math.Ceiling(0.99 * totalFrames) - 1;
        var p999Idx = (int)Math.Ceiling(0.999 * totalFrames) - 1;
        p99Idx = Math.Clamp(p99Idx, 0, totalFrames - 1);
        p999Idx = Math.Clamp(p999Idx, 0, totalFrames - 1);

        var p99FrameTime = sorted[p99Idx];
        var p999FrameTime = sorted[Math.Min(p999Idx, sorted.Count - 1)];
        var p95Idx = (int)Math.Ceiling(0.95 * totalFrames) - 1;
        p95Idx = Math.Clamp(p95Idx, 0, totalFrames - 1);
        var p95FrameTime = sorted[p95Idx];

        var onePercentLowFps = p99FrameTime > 0 ? 1000.0 / p99FrameTime : 0;
        var pointOnePercentLowFps = p999FrameTime > 0 ? 1000.0 / p999FrameTime : 0;

        // Std dev of frametime
        var variance = frameTimes.Average(f => (f - avgFrameTime) * (f - avgFrameTime));
        var stdDev = Math.Sqrt(variance);

        return new BenchmarkMetrics(
            AverageFps: Math.Round(avgFps, 2),
            OnePercentLowFps: Math.Round(onePercentLowFps, 2),
            PointOnePercentLowFps: Math.Round(pointOnePercentLowFps, 2),
            AverageFrameTimeMs: Math.Round(avgFrameTime, 3),
            P95FrameTimeMs: Math.Round(p95FrameTime, 3),
            P99FrameTimeMs: Math.Round(p99FrameTime, 3),
            StdDevFrameTimeMs: Math.Round(stdDev, 3),
            TotalFrames: totalFrames,
            DurationMs: Math.Round(durationMs, 1),
            Samples: [.. samples]);
    }

    private static char DetectDecimalSeparator(string line)
    {
        // Heuristic: if the line has a comma followed by digits where a dot would be expected, use comma
        var parts = line.Split(',');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                continue;
            // If it doesn't parse as invariant but does as pt-BR, comma is decimal separator
            if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.GetCultureInfo("pt-BR"), out _))
                return ',';
        }
        return '.';
    }

    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }
        fields.Add(current.ToString());
        return fields;
    }
}