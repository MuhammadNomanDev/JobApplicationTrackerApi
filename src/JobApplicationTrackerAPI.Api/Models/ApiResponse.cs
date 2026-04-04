using System.Text.Json.Serialization;

namespace JobApplicationTracker.Api.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public string? Message { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ApiPagination? Pagination { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Created(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message ?? "Resource created successfully." };

    public static ApiResponse<T> Fail(List<string> errors) =>
        new() { Success = false, Errors = errors };

    public static ApiResponse<T> Fail(string error) =>
        new() { Success = false, Errors = new List<string> { error } };
}

public class ApiPagination
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public ApiPagination(int page, int pageSize, int totalCount)
    {
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}
