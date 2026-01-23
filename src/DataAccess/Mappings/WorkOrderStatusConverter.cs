using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class WorkOrderStatusConverter() : ValueConverter<WorkOrderStatus, string>(v => v.Code,
    v => WorkOrderStatus.FromCode(v));

public class NullableWorkOrderStatusConverter() : ValueConverter<WorkOrderStatus?, string?>(
    v => v == null ? null : v.Code,
    v => v == null ? null : WorkOrderStatus.FromCode(v));