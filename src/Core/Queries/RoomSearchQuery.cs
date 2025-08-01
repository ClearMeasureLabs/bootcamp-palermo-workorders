namespace ProgrammingWithPalermo.ChurchBulletin.Core.Queries;

public class RoomSearchQuery(string searchPrompt)
{
    public string SearchPrompt { get; } = searchPrompt;
}