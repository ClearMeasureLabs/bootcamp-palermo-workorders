using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public interface IEntityFrameworkMapping
{
    // ReSharper disable once UnusedMemberInSuper.Global -- called via interface dispatch during EF model configuration
    void Map(ModelBuilder builder);
}