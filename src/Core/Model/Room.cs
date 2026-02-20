namespace ClearMeasure.Bootcamp.Core.Model;

public class Room : EntityBase<Room>
{
    public Room()
    {
        Name = null!;
    }

    public Room(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
    
    public override Guid Id { get; set; }
}
