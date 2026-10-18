using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Maps work-order status values to CSS classes for search-result badges.
/// </summary>
public static class WorkOrderStatusBadgeCss
{
	private static readonly Dictionary<string, string> ClassesByKey = new()
	{
		[WorkOrderStatus.Draft.Key] = "status-badge status-badge--draft",
		[WorkOrderStatus.Assigned.Key] = "status-badge status-badge--assigned",
		[WorkOrderStatus.InProgress.Key] = "status-badge status-badge--in-progress",
		[WorkOrderStatus.Complete.Key] = "status-badge status-badge--complete",
		[WorkOrderStatus.Cancelled.Key] = "status-badge status-badge--cancelled"
	};

	/// <summary>
	/// Returns the full CSS class string for a status badge, including the base <c>status-badge</c> class.
	/// </summary>
	/// <param name="status">The work-order status, or <c>null</c> for empty/None.</param>
	public static string CssClass(WorkOrderStatus? status) => CssClass(status?.Key);

	/// <summary>
	/// Returns the full CSS class string for a status key, including the base <c>status-badge</c> class.
	/// </summary>
	/// <param name="statusKey">The status key, or <c>null</c>/empty for muted.</param>
	public static string CssClass(string? statusKey)
	{
		if (string.IsNullOrEmpty(statusKey))
			return "status-badge status-badge--muted";

		return ClassesByKey.GetValueOrDefault(statusKey, "status-badge status-badge--neutral");
	}
}
