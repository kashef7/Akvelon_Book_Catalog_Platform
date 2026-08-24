using App_PL.Exceptions.CustomExceptions.Abstract;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace App_PL.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title,detail) = exception switch
        {
            AppException appEx => (appEx.StatusCode, appEx.Message,appEx.Message),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error","An unexpected error occurred. Please try again later.")
        };
        _logger.LogError(exception, "Unhandled exception occurred: {Title}", title);
        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails()
        {
            Status = status,
            Title = title,
            Detail = detail,
        }, cancellationToken);
        return true;
    }
}