namespace ClearMeasure.Bootcamp.Core;

/// <summary>
/// Opts a message property in to being recorded as a span tag when the message
/// flows through <see cref="IBus"/>. Only annotate identifiers and small scalars
/// (e.g. a work order number or status key) — never PII such as employee names
/// or unbounded payloads such as chat prompts. Tag values are truncated by the
/// bus as a safeguard.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class TelemetryTagAttribute : Attribute
{
}
