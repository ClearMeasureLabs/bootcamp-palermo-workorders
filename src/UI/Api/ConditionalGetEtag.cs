using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Weak ETag (SHA-256 over the representation bytes) for conditional GET (<c>If-None-Match</c> → 304).
/// </summary>
public static class ConditionalGetEtag
{
    /// <summary>
    /// Default JSON serialization for API GET bodies; must match <see cref="ControllerBase"/> output formatting.
    /// </summary>
    public static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Builds a weak entity-tag of the form <c>W/"{lowercase-hex}"</c> (SHA-256 of the representation) from UTF-8 bytes.
    /// </summary>
    public static EntityTagHeaderValue CreateWeakEtag(ReadOnlySpan<byte> representationUtf8)
    {
        var hash = SHA256.HashData(representationUtf8);
        var hex = Convert.ToHexStringLower(hash);
        return new EntityTagHeaderValue($"\"{hex}\"", isWeak: true);
    }

    /// <summary>
    /// Serializes <paramref name="value"/> to JSON (web defaults) and returns its weak ETag.
    /// </summary>
    public static EntityTagHeaderValue CreateWeakEtagForJson<T>(T value)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonSerializerOptions);
        return CreateWeakEtag(bytes);
    }

    /// <summary>
    /// UTF-8 encodes <paramref name="text"/> and returns its weak ETag.
    /// </summary>
    public static EntityTagHeaderValue CreateWeakEtagForPlainText(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return CreateWeakEtag(bytes);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the request's <c>If-None-Match</c> matches <paramref name="etag"/> per weak comparison.
    /// </summary>
    public static bool IfNoneMatchIncludesEtag(HttpRequest request, EntityTagHeaderValue etag)
    {
        if (!EntityTagHeaderValue.TryParseList(request.Headers.IfNoneMatch, out var candidates))
            return false;

        foreach (var candidate in candidates)
        {
            if (candidate.Equals(EntityTagHeaderValue.Any))
                return true;
            if (candidate.Compare(etag, useStrongComparison: false))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 200 OK with JSON body using <see cref="JsonSerializerOptions"/> (matches default web JSON contract).
    /// </summary>
    public static ContentResult JsonContent(object value) =>
        new()
        {
            Content = JsonSerializer.Serialize(value, JsonSerializerOptions),
            ContentType = "application/json; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
}
