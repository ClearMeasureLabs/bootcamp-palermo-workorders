// ReSharper disable PropertyCanBeMadeInitOnly.Global -- System.Text.Json set-by-convention requires mutable setters

namespace ClearMeasure.Bootcamp.Core.Model;

public class Room : EntityBase<Room>
{
    public const int NameMaxLength = 200;

    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
    public Room()
    {
        Name = null!;
    }

    public override Guid Id { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MaxLength(NameMaxLength)]
    public string Name { get; set; }
}
