namespace ClearMeasure.Bootcamp.Core;

public interface IDatabaseConfiguration
{
    string GetConnectionString();
    string GetDatabaseProvider() => "SqlServer";
}