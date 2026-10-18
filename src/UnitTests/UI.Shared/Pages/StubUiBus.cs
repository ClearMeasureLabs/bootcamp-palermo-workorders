using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

public class StubUiBus : IUiBus
{
    public void Register(IListener listener)
    {
    }

    public void UnRegister(IListener listener)
    {
    }

    public IListener<T>[] GetListeners<T>() where T : IUiBusEvent
    {
        return Array.Empty<IListener<T>>();
    }

    public void Notify<T>(T eventObject) where T : IUiBusEvent
    {
    }

    public void UnRegisterAll()
    {
    }
}