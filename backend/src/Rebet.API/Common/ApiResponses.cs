namespace Rebet.API.Common;

/// <summary>
/// Standard API success response wrapper
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Standard API error response wrapper
/// </summary>
public class ApiErrorResponse
{
    public bool Success { get; set; }
    public ErrorDetail Error { get; set; } = null!;
}

/// <summary>
/// Error detail information
/// </summary>
public class ErrorDetail
{
    public string Code { get; set; } = null!;
    public string Message { get; set; } = null!;
    public Dictionary<string, string[]>? Details { get; set; }
}

