namespace ClearMeasure.Bootcamp.Core.Model;

public class Room : EntityBase<Room>
{
    public string Name { get; set; } = string.Empty;

    public override Guid Id { get; set; }

    public override string ToString()
    {
        return Name;
    }
}
