using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;
using System.Net;

namespace Dsw2026Tpi.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Se produjo un error durante el procesamiento de la solicitud");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (status, error) = ex is AppException appEx
            ? (appEx.StatusCode, appEx.Error)
            : (HttpStatusCode.InternalServerError, new ErrorResponse(nameof(ErrorCodes.UNHANDLED_ERROR), ErrorCodes.UNHANDLED_ERROR));

        context.Response.StatusCode = (int)status;
        await context.Response.WriteAsJsonAsync(error);
    }
}
