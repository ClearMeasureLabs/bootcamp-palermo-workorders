namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Assembly anchor for <c>WebApplicationFactory&lt;TEntryPoint&gt;</c> in integration tests (avoids ambiguous <c>Program</c> types from transitive references).
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global -- used by WebApplicationFactory<TEntryPoint> via reflection in integration tests
public sealed class UiServerWebApplicationMarker;
