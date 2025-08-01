using System;
using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings
{
    public class WorkOrderStatusConverter : ValueConverter<WorkOrderStatus, string>
    {
        public WorkOrderStatusConverter() 
            : base(
                v => v.Code,
                v => WorkOrderStatus.FromCode(v),
                null)
        {
        }
    }
}