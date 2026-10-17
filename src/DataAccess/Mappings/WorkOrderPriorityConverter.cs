using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class WorkOrderPriorityConverter() : ValueConverter<WorkOrderPriority, string>(
    v => v.Key,
    v => WorkOrderPriority.FromKey(v));
