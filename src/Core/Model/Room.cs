// ReSharper disable PropertyCanBeMadeInitOnly.Global -- Qodana P5 (#9440): NHibernate proxy / System.Text.Json set-by-convention requires mutable setters

namespace ClearMeasure.Bootcamp.Core.Model;

public class Room : EntityBase<Room>
{
    public Room()
    {
        Number = null!;
        Name = null!;
    }

    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
    public Room(string number, string name)
    {
        Number = number;
        Name = name;
    }

    public override Guid Id { get; set; }

    public string Number { get; set; }

    public string Name { get; set; }

    public string DisplayName => $"{Number} – {Name}";

    public override string ToString() => DisplayName;
}
