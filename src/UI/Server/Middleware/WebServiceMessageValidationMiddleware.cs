using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Messaging;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

public sealed class WebServiceMessageValidationMiddleware
{
    /// <summary>
    /// Matches legacy <c>api/blazor-wasm-single-api</c> and versioned <c>api/v1.0/blazor-wasm-single-api</c>.
    /// </summary>
    private const string SingleApiPathSuffix = "/blazor-wasm-single-api";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            IncludeFields = false,
            PropertyNameCaseInsensitive = true
        };

    private readonly RequestDelegate _next;
    private readonly ILogger<WebServiceMessageValidationMiddleware> _logger;

    public WebServiceMessageValidationMiddleware(
        RequestDelegate next,
        ILogger<WebServiceMessageValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IValidator<WebServiceMessage> envelopeValidator,
        IServiceProvider services)
    {
        if (!IsBlazorWasmSingleApiPost(context.Request))
        {
            await _next(context);
            return;
        }

        context.Request.EnableBuffering();
        string body;
        using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync(context.RequestAborted);
        }

        context.Request.Body.Position = 0;

        WebServiceMessage? message;
        if (IsApplicationXWwwFormUrlEncoded(context.Request.ContentType))
        {
            message = TryParseWebServiceMessageFromFormUrlEncoded(body);
            if (message is null)
            {
                await WriteBadRequestAsync(
                    context,
                    "Invalid form body. Expected application/x-www-form-urlencoded fields \"typeName\" and \"body\".");
                return;
            }
        }
        else
        {
            try
            {
                message = JsonSerializer.Deserialize<WebServiceMessage>(body, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "Invalid WebServiceMessage JSON");
                await WriteBadRequestAsync(context, "Invalid request body.");
                return;
            }

            if (message is null)
            {
                await WriteBadRequestAsync(context, "Invalid request body.");
                return;
            }
        }

        var envelopeResult = await envelopeValidator.ValidateAsync(message, context.RequestAborted);
        if (!envelopeResult.IsValid)
        {
            await WriteValidationProblemAsync(context, envelopeResult.Errors);
            return;
        }

        object payload;
        try
        {
            payload = message.GetBodyObject();
        }
        catch (Exception ex) when (ex is JsonException or FormatException or TypeLoadException
            or FileNotFoundException or ArgumentNullException or InvalidOperationException)
        {
            _logger.LogDebug(ex, "Failed to deserialize payload for TypeName {TypeName}", message.TypeName);
            await WriteBadRequestAsync(context, "Invalid message payload or type.");
            return;
        }

        var validatorInterface = typeof(IValidator<>).MakeGenericType(payload.GetType());
        var payloadValidator = services.GetService(validatorInterface);
        if (payloadValidator is null)
        {
            await WriteBadRequestAsync(
                context,
                $"No validator registered for type {payload.GetType().FullName}.");
            return;
        }

        var validateMethod = validatorInterface.GetMethod(
            "ValidateAsync",
            new[] { payload.GetType(), typeof(CancellationToken) });
        if (validateMethod is null)
        {
            await WriteBadRequestAsync(context, "Validation configuration error.");
            return;
        }

        var validateTask = (Task)validateMethod.Invoke(
            payloadValidator,
            new object?[] { payload, context.RequestAborted })!;

        await validateTask.ConfigureAwait(false);

        var resultProperty = validateTask.GetType().GetProperty(nameof(Task<object>.Result))!;
        var validationResult = (ValidationResult)resultProperty.GetValue(validateTask)!;

        if (!validationResult.IsValid)
        {
            await WriteValidationProblemAsync(context, validationResult.Errors);
            return;
        }

        ReplaceRequestBodyWithNormalizedJson(context.Request, message);

        await _next(context);
    }

    /// <summary>
    /// Rewrites the request to JSON so controller model binding matches the JSON path
    /// (for example Azure DevOps service hooks posting <c>application/x-www-form-urlencoded</c>).
    /// </summary>
    private static void ReplaceRequestBodyWithNormalizedJson(HttpRequest request, WebServiceMessage message)
    {
        var normalized = JsonSerializer.Serialize(message, typeof(WebServiceMessage), JsonOptions);
        var buffer = Encoding.UTF8.GetBytes(normalized);
        var stream = new MemoryStream(buffer, writable: false);
        request.Body = stream;
        request.ContentType = "application/json; charset=utf-8";
        request.Headers.ContentLength = buffer.Length;
        stream.Position = 0;
    }

    private static bool IsApplicationXWwwFormUrlEncoded(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return MediaTypeHeaderValue.TryParse(contentType, out var parsed)
               && string.Equals(
                   parsed.MediaType,
                   "application/x-www-form-urlencoded",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static WebServiceMessage? TryParseWebServiceMessageFromFormUrlEncoded(string rawBody)
    {
        var form = QueryHelpers.ParseQuery(rawBody);
        if (!form.TryGetValue("typeName", out var typeNameValues)
            || !form.TryGetValue("body", out var bodyValues))
        {
            return null;
        }

        var typeName = typeNameValues.ToString();
        var body = bodyValues.ToString();
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(body))
        {
            return null;
        }

        return new WebServiceMessage { TypeName = typeName, Body = body };
    }

    private static bool IsBlazorWasmSingleApiPost(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method))
        {
            return false;
        }

        var path = request.Path.Value ?? "";
        return path.EndsWith(SingleApiPathSuffix, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WriteBadRequestAsync(HttpContext context, string detail)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            new ValidationProblemDetailsDto(
                Status: StatusCodes.Status400BadRequest,
                Title: "Bad Request",
                Detail: detail,
                Errors: null),
            JsonOptions,
            context.RequestAborted);
    }

    private static async Task WriteValidationProblemAsync(
        HttpContext context,
        IEnumerable<ValidationFailure> failures)
    {
        var errors = failures
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            new ValidationProblemDetailsDto(
                Status: StatusCodes.Status400BadRequest,
                Title: "One or more validation errors occurred.",
                Detail: null,
                Errors: errors),
            JsonOptions,
            context.RequestAborted);
    }

    private sealed record ValidationProblemDetailsDto(
        int Status,
        string Title,
        string? Detail,
        Dictionary<string, string[]>? Errors);
}
