using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// Generates GUIDs on demand (no persistence).
/// </summary>
[ApiController]
[Route("api/tools")]
public class GuidGeneratorToolsController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false
    };

    /// <summary>
    /// POST /api/tools/guid-generator — returns a JSON array of lowercase hyphenated GUID strings.
    /// Optional body: <c>{ "count": &lt;int&gt; }</c> (default 1, clamped 1–100). Malformed JSON yields 400.
    /// </summary>
    [HttpPost("guid-generator")]
    public async Task<IActionResult> PostAsync(CancellationToken cancellationToken = default)
    {
        int count;
        try
        {
            count = await ResolveCountAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return BadRequest();
        }

        count = Math.Clamp(count, 1, 100);
        var guids = new string[count];
        for (var i = 0; i < count; i++)
        {
            guids[i] = Guid.NewGuid().ToString("d");
        }

        return Ok(guids);
    }

    private async Task<int> ResolveCountAsync(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        Request.Body.Position = 0;

        string? json;
        using (var reader = new StreamReader(Request.Body, leaveOpen: true))
        {
            json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(json))
        {
            return 1;
        }

        var request = JsonSerializer.Deserialize<GuidGeneratorRequest>(json, JsonSerializerOptions);
        return request?.Count ?? 1;
    }

    private sealed class GuidGeneratorRequest
    {
        public int? Count { get; init; }
    }
}
