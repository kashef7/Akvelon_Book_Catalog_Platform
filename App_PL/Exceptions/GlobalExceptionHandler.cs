using App_PL.Exceptions.CustomExceptions.Abstract;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace App_PL.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            AppException appEx => (appEx.StatusCode, appEx.Message),
            _ => (StatusCodes.Status500InternalServerError, "Unhandled Exception")
        };
        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails()
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
        }, cancellationToken);
        return true;
    }
}