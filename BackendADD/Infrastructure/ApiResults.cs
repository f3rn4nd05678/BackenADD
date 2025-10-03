using BackendADD.Models;
using Microsoft.AspNetCore.Mvc;

namespace BackendADD.Infrastructure;

public static class ApiResults
{
    public static IActionResult ToResult<T>(this ControllerBase ctrl, ApiResponse<T> resp)
        => new ObjectResult(resp) { StatusCode = resp.codeStatus };

    // Atajos comunes
    public static IActionResult ApiOk<T>(this ControllerBase c, T data, string msg = "OK")
        => c.ToResult(ApiResponse<T>.Success(data, msg, 200));

    public static IActionResult ApiCreated<T>(this ControllerBase c, T data, string msg = "Created")
        => c.ToResult(ApiResponse<T>.Created(data, msg));

    public static IActionResult ApiBadRequest<T>(this ControllerBase c, string msg, T? detail = default)
        => c.ToResult(ApiResponse<T>.Fail(msg, detail, 400));

    public static IActionResult ApiNotFound(this ControllerBase c, string msg = "Not found")
        => c.ToResult(ApiResponse<object?>.Fail(msg, null, 404));
}
