using Dsw2026Tpi.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace Dsw2026Tpi.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        HttpStatusCode status;
        string message;
        switch (ex)
        {
            case ValidationDataException:
                status = HttpStatusCode.BadRequest;
                message = ex.Message;
                break;
            case EntityNotFoundException:
                status = HttpStatusCode.NotFound;
                message = ex.Message;
                break;
            case AuthenticationAppException:
                status = HttpStatusCode.Conflict;
                message = ex.Message;
                break;
            default:
                status = HttpStatusCode.InternalServerError;
                message = "Ocurrió un error inesperado al ejecutar la solicitud";
                break;
        }
        var result = JsonSerializer.Serialize(new { error = message });
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        await context.Response.WriteAsync(result);
    }
}
