using System;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands
{
	public abstract record StateCommandBase(WorkOrder WorkOrder, Employee CurrentUser) : IStateCommand
    {
        public abstract WorkOrderStatus GetBeginStatus();
        public abstract WorkOrderStatus GetEndStatus();
        protected abstract bool UserCanExecute(Employee currentUser);
        public abstract string TransitionVerbPresentTense { get; }
        public abstract string TransitionVerbPastTense { get; }

        public bool IsValid()
		{
			bool beginStatusMatches = WorkOrder.Status == GetBeginStatus();
			bool currentUserIsCorrectRole = UserCanExecute(CurrentUser);
			return beginStatusMatches && currentUserIsCorrectRole;
		}

        public bool Matches(string commandName)
		{
			return TransitionVerbPresentTense == commandName;
		}

        public virtual void Execute(StateCommandContext context)
        {
            var currentUserFullName = CurrentUser.GetFullName();
            WorkOrder.ChangeStatus(CurrentUser, context.CurrentDateTime, GetEndStatus());
        }
    }
}
