namespace BackendADD.Models;

public enum ResponseStatus { success, fail, error }

public record ApiResponse<T>(
    int codeStatus,
    ResponseStatus responseStatus,
    string message,
    T? detail
)
{
    // Factories
    public static ApiResponse<T> Success(T? detail, string message = "OK", int status = 200)
        => new(status, ResponseStatus.success, message, detail);

    public static ApiResponse<T> Created(T? detail, string message = "Created")
        => new(201, ResponseStatus.success, message, detail);

    public static ApiResponse<T> Fail(string message, T? detail = default, int status = 400)
        => new(status, ResponseStatus.fail, message, detail);

    public static ApiResponse<T> Error(string message = "Unexpected error", T? detail = default, int status = 500)
        => new(status, ResponseStatus.error, message, detail);
}
