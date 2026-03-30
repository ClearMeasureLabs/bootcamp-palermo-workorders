using System.IO;
using Microsoft.AspNetCore.Http;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Maps corrupt gzip/deflate request bodies (after <c>UseRequestDecompression</c>) to HTTP 400 instead of an unhandled 500.
/// </summary>
public sealed class InvalidCompressedRequestBodyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (IsCorruptCompression(ex))
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("The request body could not be decompressed.");
        }
    }

    private static bool IsCorruptCompression(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is InvalidDataException)
            {
                return true;
            }

            if (e is IOException && e.Message.Contains("gzip", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
