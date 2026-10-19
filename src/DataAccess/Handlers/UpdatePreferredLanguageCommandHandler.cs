using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class UpdatePreferredLanguageCommandHandler(DataContext context) :
    IRequestHandler<UpdatePreferredLanguageCommand, Employee>
{
    public async Task<Employee> Handle(UpdatePreferredLanguageCommand request,
        CancellationToken cancellationToken = default)
    {
        var employee = await context.Set<Employee>()
            .SingleAsync(e => e.UserName == request.Username, cancellationToken);

        employee.PreferredLanguage = request.PreferredLanguage;
        await context.SaveChangesAsync(cancellationToken);

        return employee;
    }
}
