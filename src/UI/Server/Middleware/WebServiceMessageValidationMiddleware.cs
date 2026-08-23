// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable NotAccessedPositionalProperty.Local
using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Messaging;
using FluentValidation;
using FluentValidation.Results;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

public sealed class WebServiceMessageValidationMiddleware(
    RequestDelegate next,
    ILogger<WebServiceMessageValidationMiddleware> logger)
{
    private const string SingleApiPathSuffix = "/blazor-wasm-single-api";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            IncludeFields = false,
            PropertyNameCaseInsensitive = true
        };

    public Task InvokeAsync(
        HttpContext context,
        IValidator<WebServiceMessage> envelopeValidator,
        IServiceProvider services) =>
        WebServiceMessageValidationPipeline.InvokeAsync(context, next, envelopeValidator, services, logger);

    internal static bool IsBlazorWasmSingleApiPost(HttpRequest request) =>
        HttpMethods.IsPost(request.Method)
        && (request.Path.Value ?? "").EndsWith(SingleApiPathSuffix, StringComparison.OrdinalIgnoreCase);

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
        if (!TryGetPayload(message, out var payload, out var payloadError))
        {
            return new PayloadValidationResult(false, Array.Empty<ValidationFailure>(), payloadError);
        }

        return await ValidatePayloadAsync(payload!, services, cancellationToken);
    }

    internal static bool TryGetPayload(WebServiceMessage message, out object? payload, out string? errorDetail)
    {
        payload = null;
        errorDetail = null;
        try
        {
            payload = message.GetBodyObject();
            return true;
        }
        catch (Exception ex) when (ex is JsonException or FormatException or TypeLoadException
            or FileNotFoundException or ArgumentNullException or InvalidOperationException)
        {
            errorDetail = "Invalid message payload or type.";
            return false;
        }
    }

    internal static async Task<PayloadValidationResult> ValidatePayloadAsync(
        object payload,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
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
            [payload.GetType(), typeof(CancellationToken)]);
        if (validateMethod is null)
        {
            return new PayloadValidationResult(false, Array.Empty<ValidationFailure>(), "Validation configuration error.");
        }

        var validateTask = (Task)validateMethod.Invoke(
            payloadValidator,
            [payload, cancellationToken])!;

        await validateTask.ConfigureAwait(false);

        var resultProperty = validateTask.GetType().GetProperty(nameof(Task<object>.Result))!;
        var validationResult = (ValidationResult)resultProperty.GetValue(validateTask)!;

        return validationResult.IsValid
            ? new PayloadValidationResult(true, Array.Empty<ValidationFailure>(), null)
            : new PayloadValidationResult(false, validationResult.Errors, null);
    }
}

internal static class WebServiceMessageValidationPipeline
{
    internal static async Task InvokeAsync(
        HttpContext context,
        RequestDelegate next,
        IValidator<WebServiceMessage> envelopeValidator,
        IServiceProvider services,
        ILogger<WebServiceMessageValidationMiddleware> logger)
    {
        if (!WebServiceMessageValidationMiddleware.IsBlazorWasmSingleApiPost(context.Request))
        {
            await next(context);
            return;
        }

        var body = await WebServiceMessageValidationMiddleware.ReadRequestBodyAsync(context.Request);
        if (!WebServiceMessageValidationMiddleware.TryDeserializeMessage(body, out var message, out var deserializeError))
        {
            logger.LogDebug("Invalid WebServiceMessage JSON: {Detail}", deserializeError);
            await WebServiceMessageValidationMiddleware.WriteBadRequestAsync(
                context,
                deserializeError ?? "Invalid request body.");
            return;
        }

        var envelopeResult = await envelopeValidator.ValidateAsync(message!, context.RequestAborted);
        if (!envelopeResult.IsValid)
        {
            await WebServiceMessageValidationMiddleware.WriteValidationProblemAsync(context, envelopeResult.Errors);
            return;
        }

        var payloadResult = await WebServiceMessagePayloadValidator.ValidateAsync(
            message!,
            services,
            context.RequestAborted);
        if (!payloadResult.IsValid)
        {
            await WritePayloadValidationFailureAsync(context, payloadResult);
            return;
        }

        await next(context);
    }

    private static async Task WritePayloadValidationFailureAsync(
        HttpContext context,
        WebServiceMessagePayloadValidator.PayloadValidationResult payloadResult)
    {
        if (payloadResult.Errors.Count > 0)
        {
            await WebServiceMessageValidationMiddleware.WriteValidationProblemAsync(context, payloadResult.Errors);
            return;
        }

        await WebServiceMessageValidationMiddleware.WriteBadRequestAsync(
            context,
            payloadResult.BadRequestDetail ?? "Invalid request body.");
    }
}
