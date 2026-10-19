using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class WorkOrderQueryHandlerCancellationTests
{
    [Test]
    public async Task Handle_WhenCancellationRequested_ThrowsOperationCanceledException()
    {
        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new WorkOrderQueryHandler(dataContext);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            handler.Handle(new WorkOrderByNumberQuery("WO-CANC"), cts.Token));
    }
}
