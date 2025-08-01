using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

public class StubUiBus : IUiBus
{
    public void Notify(object eventObject)
    {
        // Mock implementation - do nothing
    }

    public void Register(IListener listener)
    {
        // Mock implementation - do nothing
    }

    public void UnRegister(IListener listener)
    {
        // Mock implementation - do nothing
    }

    public IListener<T>[] GetListeners<T>() where T : IUiBusEvent
    {
        return Array.Empty<IListener<T>>();
    }

    public void Notify<T>(T eventObject) where T : IUiBusEvent
    {
        // Mock implementation - do nothing
    }

    public void UnRegisterAll()
    {
        // Mock implementation - do nothing
    }
}