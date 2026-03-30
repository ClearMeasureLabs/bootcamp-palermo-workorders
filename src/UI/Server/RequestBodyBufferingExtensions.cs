using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Enables request body buffering for POST, PUT, and PATCH so the body stream can be read multiple times in one request.
/// </summary>
public static class RequestBodyBufferingExtensions
{
    /// <summary>
    /// Enables buffering for the request body when <see cref="RequestBodyBufferingOptions.Enabled"/> is true.
    /// Call after <c>UseRouting</c> and before middleware or endpoints that read the body (e.g. before rate limiting that
    /// might inspect the request, and before <c>MapControllers</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Buffering runs only for <see cref="HttpMethods.Post"/>, <see cref="HttpMethods.Put"/>, and <see cref="HttpMethods.Patch"/>.
    /// If the <c>Content-Length</c> header is present and equals 0, buffering is skipped (no body to buffer).
    /// If <c>Content-Length</c> is absent (e.g. chunked transfer), buffering may still run so a body can be read.
    /// </para>
    /// <para>
    /// After enabling buffering, the request body position is reset to 0 when the stream supports seeking.
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
                context.Request.EnableBuffering(threshold);
                if (context.Request.Body.CanSeek)
                {
                    context.Request.Body.Position = 0;
                }
            }

            await next();
        });
    }

    private static bool ShouldBuffer(HttpRequest request)
    {
        var method = request.Method;
        if (!HttpMethods.IsPost(method) && !HttpMethods.IsPut(method) && !HttpMethods.IsPatch(method))
        {
            return false;
        }

        if (request.Headers.ContentLength.HasValue && request.Headers.ContentLength.Value == 0)
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
