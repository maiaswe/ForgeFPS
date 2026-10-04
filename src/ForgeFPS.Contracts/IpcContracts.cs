namespace ForgeFPS.Contracts;

/// <summary>
/// IPC message version header.
/// </summary>
public record ProtocolHeader(
    string ProtocolVersion,
    string RequestId,
    long TimestampUtc);

/// <summary>
/// IPC message contract for elevated helper communication.
/// </summary>
public record IpcMessage(
    ProtocolHeader Header,
    string ActionId,
    IpcRequestType Type,
    JsonObject? Payload);

public enum IpcRequestType
{
    Ping,
    Detect,
    Preview,
    Apply,
    Verify,
    Rollback,
    ListActions,
}

/// <summary>
/// Serializable result type for IPC responses.
/// </summary>
public record IpcResult(
    bool Success,
    string? Error,
    long ElapsedMs,
    JsonObject? Data);

/// <summary>
/// Typed dictionary used to pass structured data across IPC boundary.
/// </summary>
public class JsonObject : Dictionary<string, object?>;

/// <summary>
/// Named pipe ACL configuration constants.
/// </summary>
public static class IpcSecurityConstants
{
    public static int MaxMessageSize => 4 * 1024 * 1024; // 4MB
    public static TimeSpan TokenExpiry => TimeSpan.FromMinutes(5);
    public const string PipeNameBase = "ForgeFPS_ElevatedHelper_";
    public static int HandshakeTimeoutMs => 5000;
}