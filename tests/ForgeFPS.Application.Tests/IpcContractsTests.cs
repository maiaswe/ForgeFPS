using ForgeFPS.Contracts;
using FluentAssertions;
using Xunit;

namespace ForgeFPS.Contracts.Tests;

public class IpcContractsTests
{
    [Fact]
    public void JsonObject_ShouldAllowStringValues()
    {
        // Arrange
        var obj = new JsonObject();

        // Act
        obj["key"] = "value";
        obj["number"] = 42;
        obj["null"] = null;

        // Assert
        obj["key"].Should().Be("value");
        obj["number"].Should().Be(42);
        obj.ContainsKey("null").Should().BeTrue();
    }

    [Fact]
    public void ProtocolHeader_ShouldHaveRequiredProperties()
    {
        // Arrange
        var header = new ProtocolHeader("1.0", "req-123", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        // Assert
        header.ProtocolVersion.Should().Be("1.0");
        header.RequestId.Should().Be("req-123");
        header.TimestampUtc.Should().BeGreaterThan(0);
    }

    [Fact]
    public void IpcResult_ShouldSerializeCorrectly()
    {
        // Arrange
        var result = new IpcResult(true, null, 100, new JsonObject { ["data"] = "test" });

        // Assert
        result.Success.Should().BeTrue();
        result.ElapsedMs.Should().Be(100);
        result.Data?["data"].Should().Be("test");
    }
}