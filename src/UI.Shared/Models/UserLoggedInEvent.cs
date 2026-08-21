// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable NotAccessedPositionalProperty.Local
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

public record UserLoggedInEvent(string LoginModelUsername) : IUiBusEvent;