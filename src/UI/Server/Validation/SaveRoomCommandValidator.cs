using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using FluentValidation;

namespace ClearMeasure.Bootcamp.UI.Server.Validation;

/// <summary>
/// Validator for the client-originated <see cref="SaveRoomCommand"/> remotable request.
/// Ensures a room is present with a valid name so the WebServiceMessage validation
/// middleware finds a registered validator and lets the request through (an
/// unregistered payload type is rejected with 400).
/// </summary>
public sealed class SaveRoomCommandValidator : AbstractValidator<SaveRoomCommand>
{
    public SaveRoomCommandValidator()
    {
        RuleFor(c => c.Room)
            .NotNull()
            .DependentRules(() =>
            {
                RuleFor(c => c.Room.Name)
                    .NotEmpty()
                    .MaximumLength(Room.NameMaxLength);
            });
    }
}
