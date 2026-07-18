using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class WorkRequestStatusConverter() : ValueConverter<WorkRequestStatus, string>(v => v.Code,
    v => WorkRequestStatus.FromCode(v));