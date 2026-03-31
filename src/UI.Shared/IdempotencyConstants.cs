namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Shared HTTP header name for idempotent POST/PUT requests under <c>/api</c>.
/// </summary>
public static class IdempotencyConstants
{
    /// <summary>
    /// Request header carrying an opaque key; duplicate requests with the same key replay the first successful response.
    /// </summary>
    public const string HeaderName = "Idempotency-Key";
}
