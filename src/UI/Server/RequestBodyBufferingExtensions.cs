using Microsoft.Extensions.Options;

// ReSharper disable once UnusedMethodReturnValue.Global -- Qodana C6 (#9039): fluent
// IServiceCollection extension-method pattern; chained return value is by design, not always used.
namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Enables request body buffering for POST, PUT, and PATCH so the body stream can be read multiple times in one request.
/// </summary>
public static class RequestBodyBufferingExtensions
{
    /// <summary>
    /// Inserts middleware that calls <see cref="HttpRequestRewindExtensions.EnableBuffering(HttpRequest,int,long)"/>.
    /// when <see cref="RequestBodyBufferingOptions.Enabled"/> is true.
    /// </summary>
    public static IApplicationBuilder UseRequestBodyBuffering(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestBodyBufferingMiddleware>();
}

internal sealed class RequestBodyBufferingMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context) =>
        RequestBodyBufferingPipeline.InvokeAsync(context, next);
}

internal static class RequestBodyBufferingPipeline
{
    internal static async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var opts = context.RequestServices.GetRequiredService<IOptions<RequestBodyBufferingOptions>>().Value;
        if (opts.Enabled && RequestBodyBufferingRules.ShouldBuffer(context.Request))
        {
            var threshold = RequestBodyBufferingRules.ClampBufferThreshold(opts.BufferThreshold);
            var thresholdInt = threshold > int.MaxValue ? int.MaxValue : (int)threshold;
            context.Request.EnableBuffering(thresholdInt, long.MaxValue);
            if (context.Request.Body.CanSeek)
            {
                context.Request.Body.Position = 0;
            }
        }

        await next(context);
    }
}

internal static class RequestBodyBufferingRules
{
    internal static bool ShouldBuffer(HttpRequest request)
    {
        if (!RequestBodyBufferingRules.IsMutableMethod(request.Method))
        {
            return false;
        }

        return request.ContentLength != 0;
    }

    private static bool IsMutableMethod(string method) =>
        HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method);

    internal static long ClampBufferThreshold(long bufferThreshold) =>
        bufferThreshold < 1 ? 1 : bufferThreshold;
}
