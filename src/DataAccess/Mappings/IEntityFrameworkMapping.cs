using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public interface IEntityFrameworkMapping
{
    void Map(ModelBuilder builder);
}