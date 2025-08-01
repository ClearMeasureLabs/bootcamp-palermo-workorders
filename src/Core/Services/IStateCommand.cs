using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Services
{
	public interface IStateCommand : IRequest<StateCommandResult>, IRemotableRequest
	{
		bool IsValid();
		string TransitionVerbPresentTense { get; }
		bool Matches(string commandName);
		WorkOrderStatus GetBeginStatus();
        void Execute(StateCommandContext context);
    }
}