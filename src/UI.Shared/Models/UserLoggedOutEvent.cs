using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UI.Shared.Models;

public record UserLoggedOutEvent(string? Username) : IUiBusEvent;