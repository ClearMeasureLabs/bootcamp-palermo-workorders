using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
    /// <remarks>
    /// <para>
    /// Buffering runs only for <see cref="HttpMethods.Post"/>, <see cref="HttpMethods.Put"/>, and <see cref="HttpMethods.Patch"/>.
    /// If <see cref="HttpRequest.ContentLength"/> is 0, buffering is skipped. If it is null (for example chunked transfer), buffering still runs.
    /// </para>
    /// <para>
    /// After enabling buffering, the request body position is reset to 0 when the stream supports seeking.
    /// </para>
    /// <para>
    /// Call after <c>UseRouting</c> and before middleware or endpoints that read the body (before <c>RateLimitingMiddleware</c> and <c>MapControllers</c>).
    /// </para>
    /// </remarks>
    public static IApplicationBuilder UseRequestBodyBuffering(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var opts = context.RequestServices.GetRequiredService<IOptions<RequestBodyBufferingOptions>>().Value;
            if (opts.Enabled && ShouldBuffer(context.Request))
            {
                var threshold = ClampBufferThreshold(opts.BufferThreshold);
                var thresholdInt = threshold > int.MaxValue ? int.MaxValue : (int)threshold;
                context.Request.EnableBuffering(thresholdInt, long.MaxValue);
                if (context.Request.Body.CanSeek)
                {
                    context.Request.Body.Position = 0;
                }
            }

            await next(context);
        });
    }

    private static bool ShouldBuffer(HttpRequest request)
    {
        var method = request.Method;
        if (!HttpMethods.IsPost(method) && !HttpMethods.IsPut(method) && !HttpMethods.IsPatch(method))
        {
            return false;
        }

        if (request.ContentLength == 0)
        {
            return false;
        }

        return true;
    }

    private static long ClampBufferThreshold(long bufferThreshold)
    {
        if (bufferThreshold < 1)
        {
            return 1;
        }

        return bufferThreshold;
    }
}
