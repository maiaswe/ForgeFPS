using ForgeFPS.Domain;
using FluentAssertions;
using Xunit;

namespace ForgeFPS.Domain.Tests;

public class DomainModelTests
{
    [Fact]
    public void HardwareSnapshot_ShouldContainAllProperties()
    {
        // Arrange
        var snapshot = new HardwareSnapshot(
            new OsInfo("Win", "10", "19041", "Pro", "x64", false, false),
            [], [], 16L * 1024 * 1024 * 1024,
            [], [], "TEST-PC", false, false, DateTimeOffset.UtcNow);

        // Assert
        snapshot.MachineName.Should().Be("TEST-PC");
        snapshot.TotalMemoryBytes.Should().Be(16L * 1024 * 1024 * 1024);
    }

    [Fact]
    public void BenchmarkMetrics_ShouldCalculateFromSamples()
    {
        // Arrange
        var samples = new List<FrameSample>
        {
            new(0, 0, 16.67, 10, 12, false),
            new(1, 16.67, 15.0, 9, 11, false),
            new(2, 31.67, 18.0, 11, 13, false),
        };

        // Act
        var metrics = new BenchmarkMetrics(
            60.0, 45.0, 30.0, 16.22, 18.5, 20.0, 1.2, 3, 49.67, samples);

        // Assert
        metrics.AverageFps.Should().Be(60.0);
        metrics.TotalFrames.Should().Be(3);
        metrics.DurationMs.Should().BeApproximately(49.67, 0.01);
    }

    [Fact]
    public void RiskLevel_ShouldHaveThreeLevels()
    {
        // Assert
        ((int)RiskLevel.Low).Should().Be(0);
        ((int)RiskLevel.Moderate).Should().Be(1);
        ((int)RiskLevel.Experimental).Should().Be(2);
    }
}