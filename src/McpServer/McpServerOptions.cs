namespace ClearMeasure.Bootcamp.McpServer;

/// <summary>
/// Configuration-bound options for the standalone MCP server.
/// </summary>
public sealed class McpServerOptions
{
	/// <summary>Configuration section name (root <c>McpServer</c> in appsettings).</summary>
	public const string SectionName = "McpServer";

	/// <summary>
	/// Maximum number of work orders returned by list-work-orders (newest first).
	/// </summary>
	// ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global, PropertyCanBeMadeInitOnly.Global -- required for IOptions<T> configuration binding
	public int MaxListResults { get; set; } = 100;
}
