using ClearMeasure.Bootcamp.McpServer;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class NullDistributedBusTests
{
    [Test]
    public async Task PublishAsync_WhenEventProvided_Completes()
    {
        var bus = new NullDistributedBus();

        await Should.NotThrowAsync(async () =>
            await bus.PublishAsync(new object(), CancellationToken.None));
    }

    [Test]
    public async Task PublishAsync_WhenEventNull_Completes()
    {
        var bus = new NullDistributedBus();

        await Should.NotThrowAsync(async () =>
            await bus.PublishAsync<object>(null, CancellationToken.None));
    }
}
