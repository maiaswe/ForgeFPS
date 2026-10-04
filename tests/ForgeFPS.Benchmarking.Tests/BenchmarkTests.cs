using ForgeFPS.Benchmarking;
using ForgeFPS.Domain;
using ForgeFPS.Infrastructure.Windows;
using ForgeFPS.Shared;
using FluentAssertions;
using System.IO;
using Xunit;

namespace ForgeFPS.Benchmarking.Tests;

public class PresentMonCsvParserTests
{
    [Fact]
    public void Parse_SingleCultureCsv_ShouldComputeCorrectMetrics()
    {
        var csv = "Frame#,Timestamp_ms,FrameTime_ms,CPU_Time_ms,GPU_Time_ms,LateFrame\r\n0,0.00,16.67,10.0,12.0,0\r\n1,16.67,15.00,9.0,11.0,0\r\n2,31.67,18.00,11.0,13.0,1\r\n3,49.67,16.00,10.0,12.0,0\r\n";
        var ctx = new CaptureContext("cs2", "Counter-Strike 2", null, "1920x1080", "60", "de_dust2", System.DateTimeOffset.UtcNow);
        var metrics = PresentMonCsvParser.Parse(csv, ctx);

        metrics.TotalFrames.Should().Be(4);
        metrics.DurationMs.Should().BeApproximately(49.67, 0.1);
        metrics.AverageFps.Should().BeGreaterThan(50);
        metrics.AverageFps.Should().BeLessThan(90);
        metrics.Samples.Should().HaveCount(4);
    }

    [Fact]
    public void Parse_EmptyCsv_ShouldThrow()
    {
        var ctx = new CaptureContext("cs2", "CS2", null, "1920x1080", "60", "test", System.DateTimeOffset.UtcNow);
        var act = () => PresentMonCsvParser.Parse("", ctx);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_MissingColumn_ShouldThrow()
    {
        var csv = "Frame#,Timestamp_ms\r\n0,0\r\n";
        var ctx = new CaptureContext("cs2", "CS2", null, "1920x1080", "60", "test", System.DateTimeOffset.UtcNow);
        var act = () => PresentMonCsvParser.Parse(csv, ctx);
        act.Should().Throw<FormatException>();
    }
}

public class BenchmarkRunnerTests
{
    [Fact]
    public void AddFromCsv_ShouldParseAndStore()
    {
        var dir = Path.Combine(Path.GetTempPath(), "forgefps-bench-test");
        Directory.CreateDirectory(dir);
        var csvPath = Path.Combine(dir, "bench.csv");

        try
        {
            var csv = "Frame#,Timestamp_ms,FrameTime_ms,CPU_Time_ms,GPU_Time_ms,LateFrame\r\n0,0.00,16.67,10.0,12.0,0\r\n1,16.67,15.00,9.0,11.0,0\r\n2,31.67,18.00,11.0,13.0,0\r\n";
            File.WriteAllText(csvPath, csv);

            var runner = new BenchmarkRunner(NullLogger.Instance);
            var run = runner.AddFromCsv(csvPath, "Before", "CS2", "Safe");

            run.Metrics.TotalFrames.Should().Be(3);
            run.Metrics.AverageFps.Should().BeGreaterThan(50);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void Compare_SignificantImprovement_ShouldReturnPositive()
    {
        var runner = new BenchmarkRunner(NullLogger.Instance);
        var before = GenerateRun(runner, "before", 60.0);
        var after = GenerateRun(runner, "after", 70.0);

        var result = runner.Compare(before, after);
        result.Should().NotBeNull();
        result!.FpsImprovementPercent.Should().BePositive();
        result.IsStatisticallySignificant.Should().BeTrue();
    }

    private static ForgeFPS.Domain.BenchmarkRun GenerateRun(BenchmarkRunner runner, string id, double fpsTarget)
    {
        var dir = Path.Combine(Path.GetTempPath(), $"forgefps-bench-{id}");
        Directory.CreateDirectory(dir);
        var csvPath = Path.Combine(dir, "bench.csv");

        try
        {
            var sb = new System.Text.StringBuilder("Frame#,Timestamp_ms,FrameTime_ms,CPU_Time_ms,GPU_Time_ms,LateFrame\r\n");
            var frameTime = 1000.0 / fpsTarget;
            for (var i = 0; i < 120; i++)
            {
                var ft = frameTime + (new System.Random(i).NextDouble() - 0.5) * 2;
                sb.AppendLine($"{i},{i * frameTime:F2},{ft:F2},10,12,0");
            }
            File.WriteAllText(csvPath, sb.ToString());
            return runner.AddFromCsv(csvPath, $"{id}-run", "CS2", null);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}