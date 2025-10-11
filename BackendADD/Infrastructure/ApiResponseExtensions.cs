using Microsoft.AspNetCore.Mvc;

namespace BackendADD.Infrastructure
{
    public class ApiResponseExtensions
    {
    }
}
using Microsoft.AspNetCore.Mvc;

namespace BackendADD.Infrastructure;

// Clase de respuesta API estándar
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public int StatusCode { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Operación exitosa")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = 200
        };
    }

    public static ApiResponse<T> Created(T data, string message = "Recurso creado exitosamente")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = 201
        };
    }

    public static ApiResponse<T> Fail(string message, T? data = default, int statusCode = 400)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = data,
            StatusCode = statusCode
        };
    }
}

// Extensiones para ControllerBase
public static class ApiResponseExtensions
{
    /// <summary>
    /// Retorna una respuesta 200 OK con datos
    /// </summary>
    public static IActionResult ApiOk<T>(this ControllerBase controller, T data, string message = "Operación exitosa")
    {
        var response = ApiResponse<T>.Ok(data, message);
        return new OkObjectResult(response);
    }

    /// <summary>
    /// Retorna una respuesta 201 Created con datos
    /// </summary>
    public static IActionResult ApiCreated<T>(this ControllerBase controller, T data, string message = "Recurso creado exitosamente")
    {
        var response = ApiResponse<T>.Created(data, message);
        return new ObjectResult(response) { StatusCode = 201 };
    }

    /// <summary>
    /// Retorna una respuesta 400 Bad Request
    /// </summary>
    public static IActionResult ApiBadRequest<T>(this ControllerBase controller, string message, T? data = default)
    {
        var response = ApiResponse<T>.Fail(message, data, 400);
        return new BadRequestObjectResult(response);
    }

    /// <summary>
    /// Retorna una respuesta 404 Not Found
    /// </summary>
    public static IActionResult ApiNotFound<T>(this ControllerBase controller, string message, T? data = default)
    {
        var response = ApiResponse<T>.Fail(message, data, 404);
        return new NotFoundObjectResult(response);
    }

    /// <summary>
    /// Retorna una respuesta 404 Not Found sin datos genéricos
    /// </summary>
    public static IActionResult ApiNotFound(this ControllerBase controller, string message)
    {
        return controller.ApiNotFound<object?>(message, null);
    }

    /// <summary>
    /// Retorna una respuesta 403 Forbidden
    /// </summary>
    public static IActionResult ApiForbidden<T>(this ControllerBase controller, string message, T? data = default)
    {
        var response = ApiResponse<T>.Fail(message, data, 403);
        return new ObjectResult(response) { StatusCode = 403 };
    }

    /// <summary>
    /// Retorna una respuesta 403 Forbidden sin datos genéricos
    /// </summary>
    public static IActionResult ApiForbidden(this ControllerBase controller, string message)
    {
        return controller.ApiForbidden<object?>(message, null);
    }

    /// <summary>
    /// Retorna una respuesta 401 Unauthorized
    /// </summary>
    public static IActionResult ApiUnauthorized<T>(this ControllerBase controller, string message, T? data = default)
    {
        var response = ApiResponse<T>.Fail(message, data, 401);
        return new UnauthorizedObjectResult(response);
    }

    /// <summary>
    /// Retorna una respuesta 401 Unauthorized sin datos genéricos
    /// </summary>
    public static IActionResult ApiUnauthorized(this ControllerBase controller, string message)
    {
        return controller.ApiUnauthorized<object?>(message, null);
    }

    /// <summary>
    /// Retorna una respuesta 409 Conflict
    /// </summary>
    public static IActionResult ApiConflict<T>(this ControllerBase controller, string message, T? data = default)
    {
        var response = ApiResponse<T>.Fail(message, data, 409);
        return new ObjectResult(response) { StatusCode = 409 };
    }

    /// <summary>
    /// Retorna una respuesta 409 Conflict sin datos genéricos
    /// </summary>
    public static IActionResult ApiConflict(this ControllerBase controller, string message)
    {
        return controller.ApiConflict<object?>(message, null);
    }

    /// <summary>
    /// Retorna una respuesta 500 Internal Server Error
    /// </summary>
    public static IActionResult ApiInternalError<T>(this ControllerBase controller, string message, T? data = default)
    {
        var response = ApiResponse<T>.Fail(message, data, 500);
        return new ObjectResult(response) { StatusCode = 500 };
    }

    /// <summary>
    /// Retorna una respuesta 500 Internal Server Error sin datos genéricos
    /// </summary>
    public static IActionResult ApiInternalError(this ControllerBase controller, string message)
    {
        return controller.ApiInternalError<object?>(message, null);
    }
}