namespace ForgeFPS.Application.Tests;

using Domain;
using FluentAssertions;
using Xunit;

namespace ForgeFPS.Contracts.Tests;

public class IpcContractsTests
{
    [Fact]
    public void JsonObject_ShouldAllowStringValues()
    {
        var obj = new JsonObject();
        obj["key"] = "value";
        obj["number"] = 42;
        obj["null"] = null;

        obj["key"].Should().Be("value");
        obj["number"].Should().Be(42);
        obj.ContainsKey("null").Should().BeTrue();
    }

    [Fact]
    public void ProtocolHeader_ShouldHaveRequiredProperties()
    {
        var header = new ProtocolHeader("1.0", "req-123", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        header.ProtocolVersion.Should().Be("1.0");
        header.RequestId.Should().Be("req-123");
        header.TimestampUtc.Should().BeGreaterThan(0);
    }

    [Fact]
    public void IpcResult_ShouldSerializeCorrectly()
    {
        var result = new IpcResult(true, null, 100, new JsonObject { ["data"] = "test" });

        result.Success.Should().BeTrue();
        result.ElapsedMs.Should().Be(100);
        result.Data?["data"].Should().Be("test");
    }
}