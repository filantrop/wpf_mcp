using System.Text.Json.Serialization;

namespace WpfMcp.Server.Models;

/// <summary>
/// Standard response schema for all MCP tools.
/// </summary>
public sealed class ToolResponse<T>
{
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("error")]
    public ErrorInfo? Error { get; init; }

    [JsonPropertyName("metadata")]
    public ResponseMetadata Metadata { get; init; } = new();

    public static ToolResponse<T> Ok(T data, ResponseMetadata? metadata = null) => new()
    {
        Success = true,
        Data = data,
        Metadata = metadata ?? new ResponseMetadata()
    };

    public static ToolResponse<T> Fail(string code, string message, string? suggestion = null, bool recoverable = true) => new()
    {
        Success = false,
        Error = new ErrorInfo
        {
            Code = code,
            Message = message,
            Suggestion = suggestion,
            Recoverable = recoverable
        }
    };
}

/// <summary>
/// Error information for failed operations.
/// </summary>
public sealed class ErrorInfo
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("suggestion")]
    public string? Suggestion { get; init; }

    [JsonPropertyName("recoverable")]
    public bool Recoverable { get; init; } = true;
}

/// <summary>
/// Metadata included with all tool responses.
/// </summary>
public sealed class ResponseMetadata
{
    [JsonPropertyName("execution_time_ms")]
    public long ExecutionTimeMs { get; set; }

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; init; } = [];

    [JsonPropertyName("snapshot_valid")]
    public bool? SnapshotValid { get; set; }
}
