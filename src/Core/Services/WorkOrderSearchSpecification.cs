using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services
{
	public class WorkOrderSearchSpecification
	{
        public void MatchStatus(WorkOrderStatus? status)
		{
			Status = status;
		}

		public void MatchAssignee(Employee? assignee)
		{
			Assignee = assignee;
		}

		public void MatchCreator(Employee? creator)
		{
			Creator = creator;
		}

		public WorkOrderStatus? Status { get; private set; } = null;

        public Employee? Assignee { get; private set; } = null;

        public Employee? Creator { get; private set; } = null;
    }
}