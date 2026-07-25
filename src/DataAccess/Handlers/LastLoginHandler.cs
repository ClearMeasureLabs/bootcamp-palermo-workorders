using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class LastLoginHandler(DataContext context, ILogger<LastLoginHandler> logger)
    : INotificationHandler<UserLoggedInEvent>
{
    public async Task Handle(UserLoggedInEvent request, CancellationToken cancellationToken)
    {
        try
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
        catch (Exception ex) when (ex is DbException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Unable to record last login for {UserName}", request.UserName);
        }
    }
}
