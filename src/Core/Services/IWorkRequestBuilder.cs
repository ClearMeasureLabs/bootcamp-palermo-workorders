using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services;

public interface IWorkRequestBuilder
{
    WorkRequest CreateNewWorkRequest(Employee creator);
}