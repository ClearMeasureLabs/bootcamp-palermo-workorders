using ClearMeasure.Bootcamp.Core.Model;
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UI.Shared;

public record WorkOrderSelectedEvent(WorkOrder CurrentWorkOrder) : IUiBusEvent
{
}