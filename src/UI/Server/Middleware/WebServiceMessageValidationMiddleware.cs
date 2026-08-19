using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Messaging;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
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

        var body = await ReadRequestBodyAsync(context.Request);
        if (!TryDeserializeMessage(body, out var message, out var deserializeError))
        {
            _logger.LogDebug("Invalid WebServiceMessage JSON: {Detail}", deserializeError);
            await WriteBadRequestAsync(context, deserializeError ?? "Invalid request body.");
            return;
        }

        var envelopeResult = await envelopeValidator.ValidateAsync(message!, context.RequestAborted);
        if (!envelopeResult.IsValid)
        {
            await WriteValidationProblemAsync(context, envelopeResult.Errors);
            return;
        }

        var payloadResult = await WebServiceMessagePayloadValidator.ValidateAsync(
            message!,
            services,
            context.RequestAborted);
        if (!payloadResult.IsValid)
        {
            if (payloadResult.Errors.Count > 0)
            {
                await WriteValidationProblemAsync(context, payloadResult.Errors);
            }
            else
            {
                await WriteBadRequestAsync(context, payloadResult.BadRequestDetail ?? "Invalid request body.");
            }

            return;
        }

        await _next(context);
    }

    internal static bool IsBlazorWasmSingleApiPost(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method))
        {
            return false;
        }

        var path = request.Path.Value ?? "";
        return path.EndsWith(SingleApiPathSuffix, StringComparison.OrdinalIgnoreCase);
    }

    internal static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(request.HttpContext.RequestAborted);
        request.Body.Position = 0;
        return body;
    }

    internal static bool TryDeserializeMessage(string body, out WebServiceMessage? message, out string? errorDetail)
    {
        message = null;
        errorDetail = null;
        try
        {
            message = JsonSerializer.Deserialize<WebServiceMessage>(body, JsonOptions);
        }
        catch (JsonException)
        {
            errorDetail = "Invalid request body.";
            return false;
        }

        if (message is null)
        {
            errorDetail = "Invalid request body.";
            return false;
        }

        return true;
    }

    internal static async Task WriteBadRequestAsync(HttpContext context, string detail)
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

    internal static async Task WriteValidationProblemAsync(
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

internal static class WebServiceMessagePayloadValidator
{
    internal sealed record PayloadValidationResult(
        bool IsValid,
        IList<ValidationFailure> Errors,
        string? BadRequestDetail);

    internal static async Task<PayloadValidationResult> ValidateAsync(
        WebServiceMessage message,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        object payload;
        try
        {
            payload = message.GetBodyObject();
        }
        catch (Exception ex) when (ex is JsonException or FormatException or TypeLoadException
            or FileNotFoundException or ArgumentNullException or InvalidOperationException)
        {
            return new PayloadValidationResult(false, Array.Empty<ValidationFailure>(), "Invalid message payload or type.");
        }

        var validatorInterface = typeof(IValidator<>).MakeGenericType(payload.GetType());
        var payloadValidator = services.GetService(validatorInterface);
        if (payloadValidator is null)
        {
            return new PayloadValidationResult(
                false,
                Array.Empty<ValidationFailure>(),
                $"No validator registered for type {payload.GetType().FullName}.");
        }

        var validateMethod = validatorInterface.GetMethod(
            "ValidateAsync",
            new[] { payload.GetType(), typeof(CancellationToken) });
        if (validateMethod is null)
        {
            return new PayloadValidationResult(false, Array.Empty<ValidationFailure>(), "Validation configuration error.");
        }

        var validateTask = (Task)validateMethod.Invoke(
            payloadValidator,
            new object?[] { payload, cancellationToken })!;

        await validateTask.ConfigureAwait(false);

        var resultProperty = validateTask.GetType().GetProperty(nameof(Task<object>.Result))!;
        var validationResult = (ValidationResult)resultProperty.GetValue(validateTask)!;

        if (!validationResult.IsValid)
        {
            return new PayloadValidationResult(false, validationResult.Errors, null);
        }

        return new PayloadValidationResult(true, Array.Empty<ValidationFailure>(), null);
    }
}
