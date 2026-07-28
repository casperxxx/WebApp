using System.Net;
using Microsoft.AspNetCore.Mvc;
using WebApp.Exceptions;

namespace WebApp.Middleware;

/// <summary>
/// Middleware для глобальной обработки ошибок
/// </summary>
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
            _logger.LogError(ex, "Ошибка при обработке запроса");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = (int)HttpStatusCode.InternalServerError;
        var title = "Внутренняя ошибка сервера";
        var type = "https://tools.ietf.org/html/rfc9110#section-15.6.1";

        if (exception is NotFoundException)
        {
            statusCode = (int)HttpStatusCode.NotFound;
            title = "Не найдено";
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.5";
        }
        else if (exception is ArgumentException)
        {
            statusCode = (int)HttpStatusCode.BadRequest;
            title = "Некорректный запрос";
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.1";
        }

        var problemDetails = new ProblemDetails
        {
            Type = type,
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json");
    }
}
