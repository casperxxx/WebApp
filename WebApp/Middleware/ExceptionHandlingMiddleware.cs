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

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = (int)HttpStatusCode.InternalServerError;
        var title = "Внутренняя ошибка сервера";

        if (exception is NotFoundException)
        {
            statusCode = (int)HttpStatusCode.NotFound;
            title = "Не найдено";
        }
        else if (exception is ArgumentException)
        {
            statusCode = (int)HttpStatusCode.BadRequest;
            title = "Некорректный запрос";
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
