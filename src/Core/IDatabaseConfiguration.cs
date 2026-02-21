namespace ClearMeasure.Bootcamp.Core;

public interface IDatabaseConfiguration
{
    string GetConnectionString();
    void ResetConnectionPool();
}