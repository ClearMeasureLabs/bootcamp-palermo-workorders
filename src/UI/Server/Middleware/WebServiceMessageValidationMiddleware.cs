using System.Collections.Concurrent;
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

    private static readonly ConcurrentDictionary<Type, Func<object, IServiceProvider, CancellationToken, Task<ValidationResult?>>> PayloadValidators =
        new();

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

        var validatePayload = PayloadValidators.GetOrAdd(payload.GetType(), CreatePayloadValidator);
        var payloadResult = await validatePayload(payload, services, context.RequestAborted);

        if (payloadResult is null)
        {
            await _next(context);
            return;
        }

        if (!payloadResult.IsValid)
        {
            await WriteValidationProblemAsync(context, payloadResult.Errors);
            return;
        }

        await _next(context);
    }

    private static Func<object, IServiceProvider, CancellationToken, Task<ValidationResult?>> CreatePayloadValidator(
        Type payloadType)
    {
        var validatorInterface = typeof(IValidator<>).MakeGenericType(payloadType);
        var validateMethod = validatorInterface.GetMethod(
            "ValidateAsync",
            new[] { payloadType, typeof(CancellationToken) })
            ?? throw new InvalidOperationException(
                $"Could not resolve ValidateAsync for {validatorInterface.Name}.");

        return async (payload, services, cancellationToken) =>
        {
            var validator = services.GetService(validatorInterface);
            if (validator is null)
            {
                return null;
            }

            var task = (Task)validateMethod.Invoke(validator, new object?[] { payload, cancellationToken })!;
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty(nameof(Task<object>.Result))!;
            return (ValidationResult)resultProperty.GetValue(task)!;
        };
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
