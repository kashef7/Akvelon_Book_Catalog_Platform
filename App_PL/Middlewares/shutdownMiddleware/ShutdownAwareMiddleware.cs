namespace App_PL.Middlewares.shutdownMiddleware;

public class ShutdownAwareMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostApplicationLifetime _appLifetime;

    public ShutdownAwareMiddleware(RequestDelegate next, IHostApplicationLifetime appLifetime)
    {
        _next = next;
        _appLifetime = appLifetime;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            context.RequestAborted,
            _appLifetime.ApplicationStopping);

        context.RequestAborted = linkedCts.Token;
        await _next(context);
    }
}