using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services;

public interface IWorkOrderBuilder
{
    WorkOrder CreateNewWorkOrder(Employee creator);
}