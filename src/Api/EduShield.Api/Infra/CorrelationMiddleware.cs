namespace EduShield.Api.Infra;

public sealed class CorrelationMiddleware : IMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
    {
        var cid = ctx.Request.Headers.TryGetValue(HeaderName, out var h) && !string.IsNullOrWhiteSpace(h)
            ? h.ToString()
            : Guid.NewGuid().ToString("N");

        ctx.TraceIdentifier = cid;
        ctx.Response.Headers[HeaderName] = cid;
        await next(ctx);
    }
}



