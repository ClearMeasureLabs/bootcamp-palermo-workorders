using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

public class SessionStub : IUserSession
{
    public FlashMessage? FlashMessage;

    public Task<Employee?> GetCurrentUserAsync()
    {
        throw new NotImplementedException();
    }

    public void LogOut()
    {
        throw new NotImplementedException();
    }
}