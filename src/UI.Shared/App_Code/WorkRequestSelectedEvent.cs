using ClearMeasure.Bootcamp.Core.Model;
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UI.Shared;

public record WorkRequestSelectedEvent(WorkRequest CurrentWorkRequest) : IUiBusEvent
{
}