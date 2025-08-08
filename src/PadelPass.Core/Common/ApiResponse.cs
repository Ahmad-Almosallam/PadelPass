using System.Net;

namespace PadelPass.Core.Common;

// Core/Common/ApiResponse.cs
public class ApiResponse<T>
{
    public bool   Success { get; set; }
    public string? Message { get; set; }
    public T?      Data    { get; set; }
    public List<string>? Errors { get; set; }

    // Success factory
    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    // Failure factory
    public static ApiResponse<T> Fail(IEnumerable<string> errors) =>
        new() { Success = false, Errors = errors.ToList() };

    public static ApiResponse<T> Fail(string error) =>
        new() { Success = false, Errors = new List<string> { error } };
}

public class ApiResponse
{
    public bool   Success { get; set; }
    public string? Message { get; set; }
    public object?      Data    { get; set; }
    public List<string>? Errors { get; set; }

    // Success factory
    public static ApiResponse<object> Ok(object data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    // Failure factory
    public static ApiResponse<object> Fail(IEnumerable<string> errors) =>
        new() { Success = false, Errors = errors.ToList() };

    public static ApiResponse<object> Fail(string error) =>
        new() { Success = false, Errors = new List<string> { error } };
}

public class ErrorModel
{
    public string Key { get; set; }

    public List<string> Errors { get; set; }
}