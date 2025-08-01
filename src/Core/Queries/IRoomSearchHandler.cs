namespace ProgrammingWithPalermo.ChurchBulletin.Core.Queries;

public interface IRoomSearchHandler
{
    public string Handle(RoomSearchQuery query);
}