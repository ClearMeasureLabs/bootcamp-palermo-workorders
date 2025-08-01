using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

public class EmployeeGetAllQuery : IRequest<Employee[]>, IRemotableRequest
{
}