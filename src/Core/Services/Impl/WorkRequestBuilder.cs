using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services.Impl;

public class WorkRequestBuilder(IWorkRequestNumberGenerator numberGenerator)
    : IWorkRequestBuilder
{
    public WorkRequest CreateNewWorkRequest(Employee creator)
    {
        var workRequest = new WorkRequest
        {
            Number = numberGenerator.GenerateNumber(),
            Creator = creator,
            Status = WorkRequestStatus.Draft
        };
        return workRequest;
    }
}