using ClearMeasure.Bootcamp.Core;
using Microsoft.AspNetCore.Components;
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UI.Shared;

public class AppComponentBase : MvcComponentBase
{
    public IUiBus EventBus => base.Bus;

    [Inject]
    public new IBus Bus { get; set; } = null!;
}