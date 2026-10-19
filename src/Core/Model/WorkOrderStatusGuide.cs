namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// Explains work-order statuses and what typically happens next for volunteers.
/// </summary>
public static class WorkOrderStatusGuide
{
	/// <summary>
	/// Returns a plain-language meaning for the given status.
	/// </summary>
	public static string Explain(WorkOrderStatus status) =>
		Lookup(status).Meaning;

	/// <summary>
	/// Returns what typically happens after the given status.
	/// </summary>
	public static string WhatHappensNext(WorkOrderStatus status) =>
		Lookup(status).Next;

	private static (string Meaning, string Next) Lookup(WorkOrderStatus status) => status.Key switch
	{
		"Draft" => (
			"The work order is being written and is not assigned yet.",
			"Next it is assigned to someone who can fulfill it."),
		"Assigned" => (
			"Someone has been named to do the work, but they have not started.",
			"Next the assignee marks it in progress when they begin."),
		"InProgress" => (
			"The assignee is actively working on this order.",
			"Next they mark it complete when the work is finished."),
		"Complete" => (
			"The work is finished and no further action is needed.",
			"Nothing further; the work order is done."),
		"Cancelled" => (
			"The work order was stopped and will not be completed.",
			"Nothing further; create a new work order if the need returns."),
		_ => (
			"This status is not used for open work.",
			"No next step is defined for this status.")
	};
}
