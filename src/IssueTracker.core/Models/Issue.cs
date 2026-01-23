using System.Text.Json.Serialization;

namespace IssueTracker.Core.Models;

// This is our main Issue entity - like a JIRA ticket
public class Issue
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public IssueStatus Status { get; set; } = IssueStatus.Open;

    [JsonPropertyName("priority")]
    public IssuePriority Priority { get; set; } = IssuePriority.Medium;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

// Status enum - Open → InProgress → Done
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IssueStatus
{
    Open,
    InProgress,
    Done
}

// Priority enum
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IssuePriority
{
    Low,
    Medium,
    High
}

// Request to CREATE a new issue
public class CreateIssueRequest
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public IssuePriority Priority { get; set; } = IssuePriority.Medium;
}

// Request to UPDATE an existing issue (all fields optional)
public class UpdateIssueRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("status")]
    public IssueStatus? Status { get; set; }

    [JsonPropertyName("priority")]
    public IssuePriority? Priority { get; set; }
}

// Standard API response wrapper
public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("error")]
    public ErrorDetails? Error { get; set; }

    // Helper methods to create responses easily
    public static ApiResponse<T> Ok(T data) => new()
    {
        Success = true,
        Data = data
    };

    public static ApiResponse<T> Fail(string message, string? code = null) => new()
    {
        Success = false,
        Error = new ErrorDetails { Message = message, Code = code }
    };
}

public class ErrorDetails
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}