namespace FinGrow.Api.Middleware;

using System.Net;
using FinGrow.Domain.Errors;
using Microsoft.AspNetCore.Mvc;

public sealed class ExceptionHandlingMiddleware
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
        catch (Exception exception)
        {
            _logger.LogError(exception, "Excepcion no manejada en {Path}", context.Request.Path);
            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        var (status, title) = exception switch
        {
            DomainException => (HttpStatusCode.BadRequest, "Los datos enviados no son validos."),
            _ => (HttpStatusCode.InternalServerError, "Ocurrio un error inesperado.")
        };

        var problem = new ProblemDetails
        {
            Status = (int)status,
            Title = title,
            Detail = exception is DomainException ? exception.Message : null,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = problem.Status.Value;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}
