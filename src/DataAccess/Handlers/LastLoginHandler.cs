using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class LastLoginHandler(DataContext context) : INotificationHandler<UserLoggedInEvent>
{
    public async Task Handle(UserLoggedInEvent request, CancellationToken cancellationToken)
    {
        var employee = await context.Set<Employee>()
            .SingleOrDefaultAsync(e => e.UserName == request.UserName, cancellationToken);

        if (employee == null)
        {
            return;
        }

        employee.LastLoginUtc = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }
}
