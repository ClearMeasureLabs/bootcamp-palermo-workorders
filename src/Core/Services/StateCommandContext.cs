using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services;

public class StateCommandContext
{
	public DateTime CurrentDateTime { get; set; }
	public List<AuditEntry> AuditEntries { get; set; } = new();
}