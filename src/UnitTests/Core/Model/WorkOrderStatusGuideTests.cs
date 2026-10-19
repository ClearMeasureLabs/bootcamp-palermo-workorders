using ClearMeasure.Bootcamp.Core.Model;
using NUnit.Framework;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkOrderStatusGuideTests
{
	[Test]
	public void Explain_Draft_DescribesUnassignedWriting()
	{
		WorkOrderStatusGuide.Explain(WorkOrderStatus.Draft)
			.ShouldContain("not assigned");
	}

	[Test]
	public void WhatHappensNext_Draft_PointsToAssignment()
	{
		WorkOrderStatusGuide.WhatHappensNext(WorkOrderStatus.Draft)
			.ShouldContain("assigned");
	}

	[Test]
	public void Explain_Assigned_DescribesNamedButNotStarted()
	{
		WorkOrderStatusGuide.Explain(WorkOrderStatus.Assigned)
			.ShouldContain("named");
	}

	[Test]
	public void WhatHappensNext_Assigned_PointsToInProgress()
	{
		WorkOrderStatusGuide.WhatHappensNext(WorkOrderStatus.Assigned)
			.ShouldContain("in progress");
	}

	[Test]
	public void Explain_InProgress_DescribesActiveWork()
	{
		WorkOrderStatusGuide.Explain(WorkOrderStatus.InProgress)
			.ShouldContain("actively working");
	}

	[Test]
	public void WhatHappensNext_InProgress_PointsToComplete()
	{
		WorkOrderStatusGuide.WhatHappensNext(WorkOrderStatus.InProgress)
			.ShouldContain("complete");
	}

	[Test]
	public void Explain_Complete_DescribesFinished()
	{
		WorkOrderStatusGuide.Explain(WorkOrderStatus.Complete)
			.ShouldContain("finished");
	}

	[Test]
	public void WhatHappensNext_Complete_SaysNothingFurther()
	{
		WorkOrderStatusGuide.WhatHappensNext(WorkOrderStatus.Complete)
			.ShouldContain("Nothing further");
	}

	[Test]
	public void Explain_Cancelled_DescribesStopped()
	{
		WorkOrderStatusGuide.Explain(WorkOrderStatus.Cancelled)
			.ShouldContain("stopped");
	}

	[Test]
	public void WhatHappensNext_Cancelled_SaysNothingFurther()
	{
		WorkOrderStatusGuide.WhatHappensNext(WorkOrderStatus.Cancelled)
			.ShouldContain("Nothing further");
	}

	[Test]
	public void Explain_None_DescribesUnused()
	{
		WorkOrderStatusGuide.Explain(WorkOrderStatus.None)
			.ShouldContain("not used");
	}

	[Test]
	public void WhatHappensNext_None_SaysNoNextStep()
	{
		WorkOrderStatusGuide.WhatHappensNext(WorkOrderStatus.None)
			.ShouldContain("No next step");
	}
}
