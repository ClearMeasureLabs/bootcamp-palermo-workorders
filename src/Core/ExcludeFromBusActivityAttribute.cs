namespace ClearMeasure.Bootcamp.Core;

/// <summary>
/// Excludes a message property from automatic bus activity tags.
/// Use for free-form or sensitive values that should not be exported as telemetry.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ExcludeFromBusActivityAttribute : Attribute;
