using ClearMeasure.Bootcamp.Core;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core;

[TestFixture]
public class AuditContextTests
{
    [Test]
    public void ShouldRestoreContext_AfterNestedScope_Disposed()
    {
        var original = AuditContext.Current;
        using (AuditContext.Begin(new AuditContext("outer", CorrelationId: "request")))
        {
            using (AuditContext.Begin(AuditContext.Current with { Action = "SaveDraft" }))
            {
                AuditContext.Current.ActorUserName.ShouldBe("outer");
                AuditContext.Current.Action.ShouldBe("SaveDraft");
                AuditContext.Current.CorrelationId.ShouldBe("request");
            }

            AuditContext.Current.ActorUserName.ShouldBe("outer");
            AuditContext.Current.Action.ShouldBeNull();
        }

        AuditContext.Current.ShouldBe(original);
    }

    [Test]
    public async Task ShouldIsolateContext_BetweenConcurrentOperations()
    {
        using var outer = AuditContext.Begin(new AuditContext("parent"));
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = Task.Run(async () =>
        {
            using var scope = AuditContext.Begin(new AuditContext("first"));
            await ready.Task;
            AuditContext.Current.ActorUserName.ShouldBe("first");
        });
        var second = Task.Run(() =>
        {
            using var scope = AuditContext.Begin(new AuditContext("second"));
            ready.SetResult();
            AuditContext.Current.ActorUserName.ShouldBe("second");
        });

        await Task.WhenAll(first, second);

        AuditContext.Current.ActorUserName.ShouldBe("parent");
    }

    [Test]
    public void ShouldRestoreContext_WhenOperationThrows()
    {
        var original = AuditContext.Current;

        Should.Throw<InvalidOperationException>(() =>
        {
            using var scope = AuditContext.Begin(new AuditContext("failed"));
            throw new InvalidOperationException();
        });

        AuditContext.Current.ShouldBe(original);
    }
}
