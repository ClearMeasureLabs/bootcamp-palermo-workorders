using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using FluentValidation;

namespace ClearMeasure.Bootcamp.UI.Server.Validation;

/// <summary>
/// Validator for the client-originated <see cref="DeleteRoomCommand"/> remotable request.
/// Ensures a real room identifier is present so the WebServiceMessage validation
/// middleware finds a registered validator and lets the request through (an
/// unregistered payload type is rejected with 400).
/// </summary>
public sealed class DeleteRoomCommandValidator : AbstractValidator<DeleteRoomCommand>
{
    public DeleteRoomCommandValidator()
    {
        RuleFor(c => c.RoomId).NotEqual(Guid.Empty);
    }
}
