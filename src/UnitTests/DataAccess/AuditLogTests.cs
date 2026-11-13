using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Data.Common;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using MediatR;
using Shouldly;
using AuthenticationAuditHandler = ClearMeasure.Bootcamp.McpServer.Auditing.AuthenticationAuditHandler;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess;

[TestFixture]
public class AuditLogTests
{
    private string _path = null!;
    private DataContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        _path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"audit-{Guid.NewGuid():N}.db");
        _context = CreateContext();
        _context.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ShouldSnapshotCreationWithAllScalarsAndGeneratedGuid(bool asynchronous)
    {
        var creator = AddEmployee();
        var order = AddOrder(creator);
        order.RoomNumber = "123";
        order.Instructions = "Check wiring";
        order.CustomerRep = "Customer";
        order.DueDate = new DateOnly(2026, 12, 1);
        order.CreatedDate = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        using var scope = AuditContext.Begin(new AuditContext("trusted", "Trusted Actor", "192.0.2.1",
            "test-agent", "trace-id", "DraftSaveCommand"));

        await Save(asynchronous);

        var record = await _context.Set<AuditLog>().SingleAsync(log => log.EntityType == nameof(WorkOrder));
        record.EntityId.ShouldBe(order.Id);
        record.EntityId.ShouldNotBe(Guid.Empty);
        record.WorkOrderId.ShouldBe(order.Id);
        record.Action.ShouldBe("DraftSaveCommand");
        record.ActorUserName.ShouldBe("trusted");
        record.ActorDisplayName.ShouldBe("Trusted Actor");
        record.IpAddress.ShouldBe("192.0.2.1");
        record.UserAgent.ShouldBe("test-agent");
        record.CorrelationId.ShouldBe("trace-id");
        record.Timestamp.Kind.ShouldBe(DateTimeKind.Utc);
        record.PreviousValue.ShouldBeNull();
        var snapshot = Parse(record.NewValue);
        snapshot.GetProperty("Id").GetGuid().ShouldBe(order.Id);
        snapshot.GetProperty("CreatorId").GetGuid().ShouldBe(creator.Id);
        snapshot.GetProperty("AssigneeId").ValueKind.ShouldBe(JsonValueKind.Null);
        snapshot.GetProperty("Status").GetString().ShouldBe("DRT");
        snapshot.GetProperty("RoomNumber").GetString().ShouldBe("123");
        snapshot.GetProperty("Instructions").GetString().ShouldBe("Check wiring");
        snapshot.GetProperty("CustomerRep").GetString().ShouldBe("Customer");
        snapshot.GetProperty("DueDate").GetString().ShouldBe("2026-12-01");
        snapshot.EnumerateObject().Select(property => property.Name).Order()
            .ShouldBe(_context.Entry(order).Properties.Select(property => property.Metadata.Name).Order());
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ShouldSnapshotOnlyChangesIncludingStatusAndShadowRelationships(bool asynchronous)
    {
        var creator = AddEmployee();
        var assignee = AddEmployee("assignee");
        var order = AddOrder(creator);
        await Save(asynchronous);
        var beforeId = await _context.Set<AuditLog>().MaxAsync(log => log.Id);

        order.Title = "Changed";
        order.Assignee = assignee;
        order.Status = WorkOrderStatus.Assigned;
        await Save(asynchronous);

        var record = await _context.Set<AuditLog>().SingleAsync(log => log.Id > beforeId);
        var previous = Parse(record.PreviousValue);
        var current = Parse(record.NewValue);
        current.EnumerateObject().Select(property => property.Name).Order()
            .ShouldBe(new[] { "AssigneeId", "Status", "Title" });
        previous.GetProperty("Title").GetString().ShouldBe("Original");
        current.GetProperty("Title").GetString().ShouldBe("Changed");
        previous.GetProperty("AssigneeId").ValueKind.ShouldBe(JsonValueKind.Null);
        current.GetProperty("AssigneeId").GetGuid().ShouldBe(assignee.Id);
        previous.GetProperty("Status").GetString().ShouldBe("DRT");
        current.GetProperty("Status").GetString().ShouldBe("ASD");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ShouldNotLogReadsNoOpsOrUnchangedExplicitUpdates(bool asynchronous)
    {
        var employee = AddEmployee();
        await Save(asynchronous);
        var count = await _context.Set<AuditLog>().CountAsync();
        await _context.Set<Employee>().AsNoTracking().ToArrayAsync();
        await Save(asynchronous);
        _context.Update(employee);
        await Save(asynchronous);

        (await _context.Set<AuditLog>().CountAsync()).ShouldBe(count);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task ShouldLoadPersistedOriginalsForDetachedUpdates(bool asynchronous, bool selectiveUpdate)
    {
        var creator = AddEmployee();
        var order = AddOrder(creator);
        await Save(asynchronous);
        var cursor = await _context.Set<AuditLog>().MaxAsync(log => log.Id);
        _context.ChangeTracker.Clear();
        order.Title = "Reformatted detached order";
        await using var fresh = CreateContext();
        fresh.Attach(order);
        if (selectiveUpdate)
            fresh.Entry(order).Property(workOrder => workOrder.Title).IsModified = true;
        else
            fresh.Update(order);
        fresh.Entry(order).Property(workOrder => workOrder.Title).OriginalValue
            .ShouldBe("Reformatted detached order");
        using var scope = AuditContext.Begin(new AuditContext("system", CorrelationId: "background-trace",
            Action: "AutoReformatWorkOrder"));

        if (asynchronous)
            await fresh.SaveChangesAsync();
        else
            fresh.SaveChanges();

        var record = await fresh.Set<AuditLog>().SingleAsync(log => log.Id > cursor);
        record.EntityType.ShouldBe(nameof(WorkOrder));
        record.EntityId.ShouldBe(order.Id);
        record.ActorUserName.ShouldBe("system");
        record.Action.ShouldBe("AutoReformatWorkOrder");
        record.CorrelationId.ShouldBe("background-trace");
        var previous = Parse(record.PreviousValue);
        var current = Parse(record.NewValue);
        previous.GetProperty("Title").GetString().ShouldBe("Original");
        current.GetProperty("Title").GetString().ShouldBe("Reformatted detached order");
        current.EnumerateObject().Select(property => property.Name).ShouldBe(new[] { "Title" });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ShouldNotAuditUnchangedDetachedUpdates(bool asynchronous)
    {
        var creator = AddEmployee();
        var order = AddOrder(creator);
        await Save(asynchronous);
        var cursor = await _context.Set<AuditLog>().MaxAsync(log => log.Id);
        _context.ChangeTracker.Clear();
        await using var fresh = CreateContext();
        fresh.Update(order);

        if (asynchronous)
            await fresh.SaveChangesAsync();
        else
            fresh.SaveChanges();

        (await fresh.Set<AuditLog>().CountAsync(log => log.Id > cursor)).ShouldBe(0);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ShouldAuditEmployeeRoleAndMembershipChanges(bool asynchronous)
    {
        var employee = AddEmployee();
        var role = new Role("Technician", false, true);
        employee.Roles.Add(role);
        await Save(asynchronous);
        var addition = await _context.Set<AuditLog>().SingleAsync(log => log.EntityType == "EmployeeRoles");
        addition.EntityId.ShouldBe(employee.Id);
        Parse(addition.NewValue).GetProperty("RoleId").GetGuid().ShouldBe(role.Id);
        Parse(addition.NewValue).GetProperty("EmployeeId").GetGuid().ShouldBe(employee.Id);
        addition.PreviousValue.ShouldBeNull();

        employee.LastName = "Changed";
        role.CanCreateWorkOrder = true;
        employee.Roles.Clear();
        await Save(asynchronous);

        var removed = await _context.Set<AuditLog>().SingleAsync(log =>
            log.EntityType == "EmployeeRoles" && log.Action == "Deleted");
        removed.NewValue.ShouldBeNull();
        Parse(removed.PreviousValue).GetProperty("RoleId").GetGuid().ShouldBe(role.Id);
        var roleChange = await _context.Set<AuditLog>().SingleAsync(log =>
            log.EntityType == nameof(Role) && log.Action == "Modified");
        Parse(roleChange.NewValue).GetProperty("CanCreateWorkOrder").GetBoolean().ShouldBeTrue();
        var employeeChange = await _context.Set<AuditLog>().SingleAsync(log =>
            log.EntityType == nameof(Employee) && log.Action == "Modified");
        Parse(employeeChange.PreviousValue).GetProperty("LastName").GetString().ShouldBe("Last");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ShouldAuditActualAttachmentDeletesAndUnloadedWorkOrderCascades(bool asynchronous)
    {
        var creator = AddEmployee();
        var order = AddOrder(creator);
        var first = AddAttachment(order, creator, "first.txt");
        var second = AddAttachment(order, creator, "second.txt");
        await Save(asynchronous);
        _context.Remove(first);
        await Save(asynchronous);
        _context.ChangeTracker.Clear();
        var reloaded = await _context.Set<WorkOrder>().SingleAsync();
        _context.Remove(reloaded);

        await Save(asynchronous);

        var deleted = await _context.Set<AuditLog>()
            .Where(log => log.EntityType == nameof(WorkOrderAttachment) && log.Action == "Deleted").ToArrayAsync();
        deleted.Length.ShouldBe(2);
        deleted.Select(log => log.EntityId).ShouldContain(first.Id);
        deleted.Select(log => log.EntityId).ShouldContain(second.Id);
        deleted.ShouldAllBe(log => log.WorkOrderId == order.Id && log.NewValue == null);
        Parse(deleted.Single(log => log.EntityId == second.Id).PreviousValue)
            .GetProperty("FileName").GetString().ShouldBe("second.txt");
        (await _context.Set<WorkOrderAttachment>().CountAsync()).ShouldBe(0);
        var orderDelete = await _context.Set<AuditLog>().SingleAsync(log =>
            log.EntityType == nameof(WorkOrder) && log.Action == "Deleted");
        orderDelete.PreviousValue.ShouldNotBeNull();
        orderDelete.NewValue.ShouldBeNull();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ShouldAuditUnloadedMembershipCascade(bool asynchronous)
    {
        var employee = AddEmployee();
        employee.Roles.Add(new Role("Technician", true, true));
        await Save(asynchronous);
        _context.ChangeTracker.Clear();
        _context.Remove(await _context.Set<Role>().SingleAsync());

        await Save(asynchronous);

        (await _context.Set<AuditLog>().CountAsync(log =>
            log.EntityType == "EmployeeRoles" && log.Action == "Deleted")).ShouldBe(1);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ShouldRejectAuditModificationOrDeletionBeforeSavingBusinessChanges(bool asynchronous)
    {
        var employee = AddEmployee();
        await Save(asynchronous);
        var record = await _context.Set<AuditLog>().SingleAsync();
        employee.FirstName = "Must not persist";
        _context.Entry(record).Property(log => log.Action).CurrentValue = "Tampered";

        await Should.ThrowAsync<InvalidOperationException>(() => Save(asynchronous));
        _context.Entry(record).State = EntityState.Unchanged;
        _context.Remove(record);
        await Should.ThrowAsync<InvalidOperationException>(() => Save(asynchronous));

        await using var verify = CreateContext();
        (await verify.Set<Employee>().SingleAsync()).FirstName.ShouldBe("First");
        (await verify.Set<AuditLog>().SingleAsync()).Action.ShouldBe("Created");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ShouldRollbackBusinessWriteWhenAuditInsertFailsAndAllowCleanRetry(bool asynchronous)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER FailAudit BEFORE INSERT ON AuditLog BEGIN SELECT RAISE(ABORT, 'audit unavailable'); END;");
        AddEmployee();

        await Should.ThrowAsync<DbUpdateException>(() => Save(asynchronous));

        await using (var verify = CreateContext())
        {
            (await verify.Set<Employee>().CountAsync()).ShouldBe(0);
            (await verify.Set<AuditLog>().CountAsync()).ShouldBe(0);
        }
        _context.ChangeTracker.Entries<AuditLog>().ShouldBeEmpty();
        await _context.Database.ExecuteSqlRawAsync("DROP TRIGGER FailAudit;");
        await Save(asynchronous);
        (await _context.Set<Employee>().CountAsync()).ShouldBe(1);
        (await _context.Set<AuditLog>().CountAsync()).ShouldBe(1);
    }

    [Test]
    public async Task ShouldRollbackAuditsWithExplicitBusinessTransaction()
    {
        await using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            AddEmployee();
            await _context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var verify = CreateContext();
        (await verify.Set<Employee>().CountAsync()).ShouldBe(0);
        (await verify.Set<AuditLog>().CountAsync()).ShouldBe(0);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task ShouldNotReplayGeneratedHistoryWhenAcceptanceIsDeferred(bool asynchronous, bool changeAgain)
    {
        var creator = AddEmployee();
        var order = AddOrder(creator);
        await Save(asynchronous);
        order.Title = "First change";

        await SaveWithoutAccepting(asynchronous);

        _context.Entry(order).State.ShouldBe(EntityState.Modified);
        _context.Entry(order).Property(workOrder => workOrder.Title).OriginalValue.ShouldBe("Original");
        _context.ChangeTracker.Entries<AuditLog>().ShouldBeEmpty();
        if (changeAgain) order.Title = "Second change";
        await Save(asynchronous);
        var history = await _context.Set<AuditLog>().Where(log =>
            log.EntityType == nameof(WorkOrder) && log.Action == "Modified").OrderBy(log => log.Id).ToArrayAsync();
        history.Length.ShouldBe(changeAgain ? 2 : 1);
        Parse(history[0].PreviousValue).GetProperty("Title").GetString().ShouldBe("Original");
        Parse(history[0].NewValue).GetProperty("Title").GetString().ShouldBe("First change");
        if (changeAgain)
        {
            Parse(history[1].PreviousValue).GetProperty("Title").GetString().ShouldBe("First change");
            Parse(history[1].NewValue).GetProperty("Title").GetString().ShouldBe("Second change");
        }
    }

    [Test]
    public async Task ShouldPermitExplicitAcceptanceAfterSaveWithoutReplayingCreation()
    {
        AddEmployee();

        await _context.SaveChangesAsync(false);
        _context.ChangeTracker.AcceptAllChanges();
        await _context.SaveChangesAsync();

        (await _context.Set<Employee>().CountAsync()).ShouldBe(1);
        (await _context.Set<AuditLog>().CountAsync()).ShouldBe(1);
    }

    [Test]
    public async Task ShouldDiscardUnacceptedSnapshotAfterTransactionRollback()
    {
        var creator = AddEmployee();
        var order = AddOrder(creator);
        await _context.SaveChangesAsync();
        await using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            order.Title = "Changed";
            await _context.SaveChangesAsync(false);
            await transaction.RollbackAsync();
        }

        await _context.SaveChangesAsync();

        var record = await _context.Set<AuditLog>().SingleAsync(log =>
            log.EntityType == nameof(WorkOrder) && log.Action == "Modified");
        Parse(record.PreviousValue).GetProperty("Title").GetString().ShouldBe("Original");
        Parse(record.NewValue).GetProperty("Title").GetString().ShouldBe("Changed");
    }

    [Test]
    public async Task ShouldNeverExposeAuditSnapshotValuesInEfCommandLogs()
    {
        await _context.DisposeAsync();
        var messages = new List<string>();
        _context = new LoggingDataContext(new StubDatabaseConfiguration(_path), messages);
        _context.Add(new Employee("private-audit-actor", "Private", "Actor", "private-audit@example.test"));

        await _context.SaveChangesAsync();

        messages.ShouldContain(message => message.Contains("INSERT INTO \"AuditLog\"", StringComparison.Ordinal));
        messages.ShouldNotContain(message => message.Contains("private-audit-actor", StringComparison.Ordinal));
        messages.ShouldNotContain(message => message.Contains("private-audit@example.test", StringComparison.Ordinal));
        (await _context.Set<AuditLog>().SingleAsync()).NewValue!.ShouldContain("private-audit@example.test");
    }

    [Test]
    public async Task ShouldBoundReadsFilterAndUseExclusiveKeysetWithoutTracking()
    {
        var entityId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddMinutes(-1);
        _context.AddRange(Enumerable.Range(0, 205).Select(index => new AuditLog
        {
            Action = "Read",
            EntityType = nameof(WorkOrder),
            EntityId = entityId,
            Timestamp = from.AddSeconds(1)
        }));
        _context.Add(new AuditLog { Action = "Other", EntityType = nameof(Employee), EntityId = entityId });
        _context.Add(new AuditLog { Action = "Old", EntityType = nameof(WorkOrder), EntityId = entityId,
            Timestamp = from.AddDays(-1) });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var service = new AuditLogService(_context);

        var first = await service.ReadAsync(nameof(WorkOrder), entityId, from, null);
        var second = await service.ReadAsync(nameof(WorkOrder), entityId, from, first[^1].Id);

        first.Length.ShouldBe(200);
        second.Length.ShouldBe(5);
        first.Select(log => log.Id).ShouldBe(first.Select(log => log.Id).OrderDescending());
        var cursor = first[^1].Id;
        second.ShouldAllBe(log => log.Id < cursor);
        _context.ChangeTracker.Entries().ShouldBeEmpty();
        (await service.ReadAsync(null, Guid.NewGuid(), null, null)).ShouldBeEmpty();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ShouldRoundTripUtcTimestampWithJsonUtcSuffixInFreshUntrackedContext(bool asynchronous)
    {
        AddEmployee();
        await Save(asynchronous);
        await using var verify = CreateContext();

        var record = await verify.Set<AuditLog>().AsNoTracking().SingleAsync();
        var json = JsonSerializer.Serialize(record, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        record.Timestamp.Kind.ShouldBe(DateTimeKind.Utc);
        Parse(json).GetProperty("timestamp").GetString()!.ShouldEndWith("Z");
        verify.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Test]
    public async Task ShouldWriteExplicitEventsBoundMetadataAndPurgeOnlyBeforeUtcCutoff()
    {
        var service = new AuditLogService(_context);
        var entityId = Guid.NewGuid();
        using (AuditContext.Begin(new AuditContext(new string('a', 350), new string('b', 600),
                   new string('c', 80), new string('d', 600), new string('e', 150))))
            await service.WriteAsync("Viewed", nameof(WorkOrder), entityId, newValue: "{\"viewed\":true}");
        var record = await _context.Set<AuditLog>().SingleAsync();
        record.ActorUserName.Length.ShouldBe(255);
        record.ActorDisplayName!.Length.ShouldBe(500);
        record.IpAddress!.Length.ShouldBe(45);
        record.UserAgent!.Length.ShouldBe(500);
        record.CorrelationId!.Length.ShouldBe(64);
        record.WorkOrderId.ShouldBe(entityId);
        var cutoff = DateTime.UtcNow.AddDays(-30);
        _context.AddRange(
            new AuditLog { Action = "Old", EntityType = "Test", Timestamp = cutoff.AddTicks(-1) },
            new AuditLog { Action = "Boundary", EntityType = "Test", Timestamp = cutoff });
        await _context.SaveChangesAsync();

        (await service.PurgeAsync(cutoff)).ShouldBe(1);

        (await _context.Set<AuditLog>().CountAsync()).ShouldBe(2);
        (await _context.Set<AuditLog>().AnyAsync(log => log.Action == "Boundary")).ShouldBeTrue();
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => service.PurgeAsync(DateTime.Now));
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => service.PurgeAsync(DateTime.UtcNow.AddDays(1)));
    }

    [TestCase(nameof(AuditLog.Action), 255)]
    [TestCase(nameof(AuditLog.EntityType), 255)]
    [TestCase(nameof(AuditLog.ActorUserName), 255)]
    [TestCase(nameof(AuditLog.ActorDisplayName), 500)]
    [TestCase(nameof(AuditLog.IpAddress), 45)]
    [TestCase(nameof(AuditLog.UserAgent), 500)]
    [TestCase(nameof(AuditLog.CorrelationId), 64)]
    public void ShouldMapSpecifiedAuditMetadataLimits(string propertyName, int length)
    {
        _context.Model.FindEntityType(typeof(AuditLog))!.FindProperty(propertyName)!.GetMaxLength()
            .ShouldBe(length);
    }

    [Test]
    public void ShouldMapActionAndDescendingTimestampIndexesWithUtcDefault()
    {
        var entity = _context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(AuditLog))!;

        entity.GetIndexes().ShouldContain(index => index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(AuditLog.Action));
        var timestampIndex = entity.GetIndexes().Single(index =>
            index.Properties[0].Name == nameof(AuditLog.Timestamp));
        // EF normalizes an all-descending index to an empty direction list.
        timestampIndex.IsDescending.ShouldBe(Array.Empty<bool>());
        entity.FindProperty(nameof(AuditLog.Timestamp))!.GetDefaultValueSql().ShouldBe("CURRENT_TIMESTAMP");
        entity.FindProperty(nameof(AuditLog.WorkOrderId))!.ClrType.ShouldBe(typeof(Guid?));
        entity.FindProperty(nameof(AuditLog.EntityId))!.ClrType.ShouldBe(typeof(Guid?));
        entity.FindProperty(nameof(AuditLog.Id))!.ClrType.ShouldBe(typeof(long));
    }

    [Test]
    public async Task ShouldBoundExplicitActionAndEntityNamesToSpecifiedLimits()
    {
        await new AuditLogService(_context).WriteAsync(new string('a', 300), new string('b', 300));

        var record = await _context.Set<AuditLog>().SingleAsync();
        record.Action.Length.ShouldBe(255);
        record.EntityType.Length.ShouldBe(255);
    }

    [TestCase("trusted-actor", "trusted-actor")]
    [TestCase("system", "demo:untrusted-command-user")]
    [TestCase("anonymous", "demo:untrusted-command-user")]
    public async Task ShouldScopeStateCommandActionWithTrustedOrExplicitDemoActor(string actor, string expectedActor)
    {
        var creator = AddEmployee();
        var order = AddOrder(creator);
        await _context.SaveChangesAsync();
        order.Title = "Changed by command";
        var suppliedUser = new Employee("untrusted-command-user", "Wrong", "Actor", "wrong@example.test");
        var handler = new StateCommandHandler(_context, TimeProvider.System, new StubDistributedBus(),
            NullLogger<StateCommandHandler>.Instance);
        using var scope = AuditContext.Begin(new AuditContext(actor, "Trusted Name",
            "192.0.2.1", "request-agent", "request-trace", "OuterAction"));

        await handler.Handle(new SaveDraftCommand(order, suppliedUser));

        var record = await _context.Set<AuditLog>().SingleAsync(log =>
            log.EntityType == nameof(WorkOrder) && log.Action == nameof(SaveDraftCommand));
        record.ActorUserName.ShouldBe(expectedActor);
        record.ActorDisplayName.ShouldBe("Trusted Name");
        record.IpAddress.ShouldBe("192.0.2.1");
        record.UserAgent.ShouldBe("request-agent");
        record.CorrelationId.ShouldBe("request-trace");
        AuditContext.Current.Action.ShouldBe("OuterAction");
    }

    [TestCase(nameof(SaveDraftCommand), "DRT", "DRT")]
    [TestCase(nameof(DraftToAssignedCommand), "DRT", "ASD")]
    [TestCase(nameof(AssignedToInProgressCommand), "ASD", "IPG")]
    [TestCase(nameof(InProgressToAssignedCommand), "IPG", "ASD")]
    [TestCase(nameof(InProgressToCompleteCommand), "IPG", "CMP")]
    [TestCase(nameof(AssignedToCancelledCommand), "ASD", "CNL")]
    public async Task ShouldAuditEveryStateCommandWithTrustedActorAndChangedValues(
        string commandName, string beforeStatus, string afterStatus)
    {
        var actor = AddEmployee();
        var order = AddOrder(actor);
        order.Assignee = actor;
        order.Status = WorkOrderStatus.FromCode(beforeStatus);
        order.CreatedDate = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        order.AssignedDate = beforeStatus == "DRT" ? null : order.CreatedDate;
        await _context.SaveChangesAsync();
        order.Title = "Changed by state command";
        StateCommandBase command = commandName switch
        {
            nameof(SaveDraftCommand) => new SaveDraftCommand(order, actor),
            nameof(DraftToAssignedCommand) => new DraftToAssignedCommand(order, actor),
            nameof(AssignedToInProgressCommand) => new AssignedToInProgressCommand(order, actor),
            nameof(InProgressToAssignedCommand) => new InProgressToAssignedCommand(order, actor),
            nameof(InProgressToCompleteCommand) => new InProgressToCompleteCommand(order, actor),
            nameof(AssignedToCancelledCommand) => new AssignedToCancelledCommand(order, actor),
            _ => throw new ArgumentOutOfRangeException(nameof(commandName))
        };
        command.IsValid().ShouldBeTrue();
        var handler = new StateCommandHandler(_context, TimeProvider.System, new StubDistributedBus(),
            NullLogger<StateCommandHandler>.Instance);
        using var scope = AuditContext.Begin(new AuditContext("authenticated-actor", "Authenticated Actor",
            "192.0.2.1", "request-agent", "state-command-trace"));

        var result = await handler.Handle(command);

        result.WorkOrder.Status.Code.ShouldBe(afterStatus);
        var record = await _context.Set<AuditLog>().SingleAsync(log =>
            log.EntityType == nameof(WorkOrder) && log.EntityId == order.Id && log.Action == commandName);
        record.ActorUserName.ShouldBe("authenticated-actor");
        record.ActorDisplayName.ShouldBe("Authenticated Actor");
        record.IpAddress.ShouldBe("192.0.2.1");
        record.UserAgent.ShouldBe("request-agent");
        record.CorrelationId.ShouldBe("state-command-trace");
        var previous = Parse(record.PreviousValue);
        var current = Parse(record.NewValue);
        previous.GetProperty("Title").GetString().ShouldBe("Original");
        current.GetProperty("Title").GetString().ShouldBe("Changed by state command");
        if (beforeStatus != afterStatus)
        {
            previous.GetProperty("Status").GetString().ShouldBe(beforeStatus);
            current.GetProperty("Status").GetString().ShouldBe(afterStatus);
        }
        else
            current.TryGetProperty("Status", out _).ShouldBeFalse();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ShouldAttributeStateCommandFallbackToExplicitDemoIdentity(bool anonymousRequest)
    {
        var creator = AddEmployee();
        var order = AddOrder(creator);
        await _context.SaveChangesAsync();
        order.Title = "Changed by demo actor";
        var handler = new StateCommandHandler(_context, TimeProvider.System, new StubDistributedBus(),
            NullLogger<StateCommandHandler>.Instance);
        using var scope = anonymousRequest
            ? AuditContext.Begin(new AuditContext("anonymous", CorrelationId: "request-trace"))
            : null;

        await handler.Handle(new SaveDraftCommand(order, creator));

        var record = await _context.Set<AuditLog>().SingleAsync(log =>
            log.EntityType == nameof(WorkOrder) && log.Action == nameof(SaveDraftCommand));
        record.ActorUserName.ShouldBe("demo:actor");
        record.CorrelationId.ShouldBe(anonymousRequest ? "request-trace" : null);
    }

    [TestCase("anonymous", "demo:actor")]
    [TestCase("trusted-actor", "trusted-actor")]
    [TestCase("system", "demo:actor")]
    public async Task ShouldScopeDatedCreationWithCreatorAndPreserveTrustedMetadata(string actor, string expectedActor)
    {
        AddEmployee();
        AddEmployee("assignee");
        await _context.SaveChangesAsync();
        var handler = new CreateDatedWorkOrdersHandler(_context, new StubBus(),
            new StubWorkOrderNumberGenerator(), TimeProvider.System,
            NullLogger<CreateDatedWorkOrdersHandler>.Instance);
        using var scope = AuditContext.Begin(new AuditContext(actor, CorrelationId: "request-trace"));
        var command = new CreateDatedWorkOrdersCommand("actor", "assignee", "Batch", "Description",
            [new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 2)]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.ShouldBeTrue();
        var records = await _context.Set<AuditLog>().Where(log =>
            log.EntityType == nameof(WorkOrder) && log.Action == nameof(CreateDatedWorkOrdersCommand)).ToArrayAsync();
        records.Length.ShouldBe(2);
        records.ShouldAllBe(log => log.ActorUserName == expectedActor && log.CorrelationId == "request-trace");
        AuditContext.Current.ActorUserName.ShouldBe(actor);
    }

    [TestCase("anonymous", "demo:actor")]
    [TestCase("trusted-actor", "trusted-actor")]
    [TestCase("system", "demo:actor")]
    public async Task ShouldScopeAttachmentMetadataWithUploaderAndPreserveTrustedMetadata(string actor, string expectedActor)
    {
        var uploader = AddEmployee();
        var order = AddOrder(uploader);
        await _context.SaveChangesAsync();
        var handler = new AddAttachmentMetadataCommandHandler(_context, TimeProvider.System);
        using var scope = AuditContext.Begin(new AuditContext(actor, CorrelationId: "request-trace"));

        var attachment = await handler.Handle(new AddAttachmentMetadataCommand(
            order, uploader, "evidence.txt", "text/plain", 10));

        var record = await _context.Set<AuditLog>().SingleAsync(log => log.EntityId == attachment.Id);
        record.Action.ShouldBe(nameof(AddAttachmentMetadataCommand));
        record.ActorUserName.ShouldBe(expectedActor);
        record.CorrelationId.ShouldBe("request-trace");
        Parse(record.NewValue).GetProperty("UploadedById").GetGuid().ShouldBe(uploader.Id);
        AuditContext.Current.ActorUserName.ShouldBe(actor);
    }

    [TestCase("system", "reported", "demo:reported", false)]
    [TestCase("anonymous", "reported", "demo:reported", false)]
    [TestCase("trusted-actor", "reported", "trusted-actor", false)]
    [TestCase("anonymous", "reported", "demo:reported", true)]
    [TestCase("trusted-actor", "reported", "trusted-actor", true)]
    [TestCase("anonymous", null, "anonymous", true)]
    public async Task ShouldAuditAuthenticationPreservingHttpMetadataAndDistinguishingDemoIdentity(
        string ambientActor, string? reportedActor, string expectedActor, bool logout)
    {
        var handler = new AuthenticationAuditHandler(new AuditLogService(_context));
        using var scope = AuditContext.Begin(new AuditContext(ambientActor, IpAddress: "192.0.2.1",
            UserAgent: "request-agent", CorrelationId: "request-trace", Action: "OuterAction"));

        if (logout)
            await handler.Handle(new UserLoggedOutEvent(reportedActor), CancellationToken.None);
        else
            await handler.Handle(new UserLoggedInEvent(reportedActor!), CancellationToken.None);

        var record = await _context.Set<AuditLog>().SingleAsync();
        record.Action.ShouldBe(logout ? "Logout" : "Login");
        record.EntityType.ShouldBe("Authentication");
        record.ActorUserName.ShouldBe(expectedActor);
        record.IpAddress.ShouldBe("192.0.2.1");
        record.UserAgent.ShouldBe("request-agent");
        record.CorrelationId.ShouldBe("request-trace");
        AuditContext.Current.ActorUserName.ShouldBe(ambientActor);
        AuditContext.Current.Action.ShouldBe("OuterAction");
    }

    [Test]
    public async Task ShouldPropagateAuthenticationAuditCancellationAndRestoreAmbientContext()
    {
        var handler = new AuthenticationAuditHandler(new AuditLogService(_context));
        using var scope = AuditContext.Begin(new AuditContext("anonymous", CorrelationId: "request-trace"));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            handler.Handle(new UserLoggedInEvent("reported"), cancellation.Token));

        AuditContext.Current.ActorUserName.ShouldBe("anonymous");
        AuditContext.Current.CorrelationId.ShouldBe("request-trace");
        _context.ChangeTracker.Entries<AuditLog>().ShouldBeEmpty();
        await using var verify = CreateContext();
        (await verify.Set<AuditLog>().CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task ShouldKeepAuthenticationTelemetryFreeOfReportedIdentity()
    {
        long measurements = 0;
        var tags = new List<KeyValuePair<string, object?>>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == "ChurchBulletin.Application" && instrument.Name == "app.user.logins")
                meterListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, count, measurementTags, _) =>
        {
            measurements += count;
            tags.AddRange(measurementTags.ToArray());
        });
        listener.Start();
        var logger = new StubTelemetryLogger();

        await new TelemetryHandler(logger).Handle(new UserLoggedInEvent("private-auth-subject"), CancellationToken.None);

        measurements.ShouldBe(1);
        tags.ShouldBeEmpty();
        logger.Messages.ShouldBe(new[] { "Recorded login metric" });
        logger.StructuredValues.ShouldNotContain(value => value.Contains("private-auth-subject", StringComparison.Ordinal));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ShouldAddOnlyOneAuditInsertAndNoSnapshotReadCommandsPerUpdate(bool asynchronous)
    {
        await _context.DisposeAsync();
        var counter = new StubCommandCounter();
        _context = new CountingDataContext(new StubDatabaseConfiguration(_path), counter);
        var creator = AddEmployee();
        var order = AddOrder(creator);
        await Save(asynchronous);
        counter.Commands.Clear();
        order.Title = "Updated";

        await Save(asynchronous);

        counter.Commands.Count.ShouldBe(2);
        counter.Commands.Count(command => command.Contains("INSERT INTO \"AuditLog\"", StringComparison.Ordinal))
            .ShouldBe(1);
        counter.Commands.Count(command => command.Contains("UPDATE \"WorkOrder\"", StringComparison.Ordinal))
            .ShouldBe(1);
        counter.Commands.ShouldAllBe(command => !command.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase));
        counter.Commands.Clear();
        await Save(asynchronous);
        counter.Commands.ShouldBeEmpty();
        TestContext.Out.WriteLine(
            "SQLite update: 1 domain UPDATE + 1 audit INSERT, 0 snapshot SELECT commands; " +
            "no-op: 0 commands. Provider transaction protocol roundtrips are not measured.");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ShouldLoadExactlyOnePersistedSnapshotForDetachedEmployeeUpdate(bool asynchronous)
    {
        await _context.DisposeAsync();
        var counter = new StubCommandCounter();
        _context = new CountingDataContext(new StubDatabaseConfiguration(_path), counter);
        var employee = AddEmployee();
        await Save(asynchronous);
        _context.ChangeTracker.Clear();
        employee.LastName = "Changed";
        _context.Attach(employee);
        _context.Entry(employee).Property(person => person.LastName).IsModified = true;
        counter.Commands.Clear();

        await Save(asynchronous);

        counter.Commands.Count.ShouldBe(3);
        counter.Commands.Count(command => command.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            .ShouldBe(1);
        counter.Commands.Count(command => command.Contains("INSERT INTO \"AuditLog\"", StringComparison.Ordinal))
            .ShouldBe(1);
        counter.Commands.Count(command => command.Contains("UPDATE \"Employee\"", StringComparison.Ordinal))
            .ShouldBe(1);
    }

    [Test]
    public async Task ShouldReportRepeatableRelativeSaveChangesCostWithoutTimingGate()
    {
        var employee = AddEmployee();
        var order = AddOrder(employee);
        await _context.SaveChangesAsync();
        var baselinePath = _path + ".baseline";
        try
        {
            await using var baseline = new BaselineContext(baselinePath);
            await baseline.Database.EnsureCreatedAsync();
            var baselineEmployee = new Employee("baseline", "First", "Last", "baseline@example.test");
            var baselineOrder = new WorkOrder { Number = "WO-001", Title = "Original", Creator = baselineEmployee };
            baseline.Add(baselineOrder);
            await baseline.SaveChangesAsync();
            var auditedSamples = new List<double>();
            var baselineSamples = new List<double>();
            for (var round = 0; round < 4; round++)
            {
                var baselineMs = await Measure(baseline, baselineOrder, round);
                var auditedMs = await Measure(_context, order, round);
                if (round == 0) continue;
                baselineSamples.Add(baselineMs);
                auditedSamples.Add(auditedMs);
            }
            var baselineMedian = baselineSamples.Order().ElementAt(1);
            var auditedMedian = auditedSamples.Order().ElementAt(1);
            TestContext.Out.WriteLine(
                $"SQLite 3x30 updates after warmup: median baseline={baselineMedian:F2}ms, " +
                $"audited={auditedMedian:F2}ms, ratio={auditedMedian / baselineMedian:F2}x; " +
                "not a representative SQL Server benchmark or a <5% overhead claim; no timing threshold.");
            (await _context.Set<AuditLog>().CountAsync(log => log.EntityType == nameof(WorkOrder)))
                .ShouldBe(121);
            await baseline.Database.EnsureDeletedAsync();
        }
        finally
        {
            if (File.Exists(baselinePath)) File.Delete(baselinePath);
        }
    }

    private static async Task<double> Measure(DbContext context, WorkOrder order, int round)
    {
        var watch = Stopwatch.StartNew();
        for (var index = 0; index < 30; index++)
        {
            order.Title = $"round-{round}-update-{index}";
            await context.SaveChangesAsync();
        }
        return watch.Elapsed.TotalMilliseconds;
    }

    private DataContext CreateContext() => new(new StubDatabaseConfiguration(_path));

    private Employee AddEmployee(string userName = "actor")
    {
        var employee = new Employee(userName, "First", "Last", $"{userName}@example.test");
        _context.Add(employee);
        return employee;
    }

    private WorkOrder AddOrder(Employee creator)
    {
        var order = new WorkOrder { Number = "WO-001", Title = "Original", Creator = creator };
        _context.Add(order);
        return order;
    }

    private static WorkOrderAttachment AddAttachment(WorkOrder order, Employee employee, string fileName)
    {
        var attachment = new WorkOrderAttachment
        {
            WorkOrderId = order.Id,
            FileName = fileName,
            UploadedBy = employee,
            UploadedById = employee.Id,
            FileSize = 42,
            ContentType = "text/plain",
            UploadedDate = DateTime.UtcNow
        };
        order.Attachments.Add(attachment);
        return attachment;
    }

    private Task Save(bool asynchronous)
    {
        if (asynchronous) return _context.SaveChangesAsync();
        _context.SaveChanges();
        return Task.CompletedTask;
    }

    private Task SaveWithoutAccepting(bool asynchronous)
    {
        if (asynchronous) return _context.SaveChangesAsync(false);
        _context.SaveChanges(false);
        return Task.CompletedTask;
    }

    private static JsonElement Parse(string? json) => JsonSerializer.Deserialize<JsonElement>(json!);

    private sealed class StubDatabaseConfiguration(string path) : IDatabaseConfiguration
    {
        public string GetConnectionString() => $"Data Source={path}";
        public void ResetConnectionPool() { }
    }

    private sealed class StubDistributedBus : IDistributedBus
    {
        public Task PublishAsync<TEvent>(TEvent? @event, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubTelemetryLogger : ILogger<TelemetryHandler>
    {
        internal List<string> Messages { get; } = [];
        internal List<string> StructuredValues { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            if (state is IEnumerable<KeyValuePair<string, object?>> properties)
                StructuredValues.AddRange(properties.Select(property => $"{property.Key}={property.Value}"));
        }
    }

    private sealed class StubBus : IBus
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request) => throw new NotSupportedException();
        public Task<object?> Send(object request) => throw new NotSupportedException();
        public Task Publish(INotification notification) => Task.CompletedTask;
    }

    private sealed class StubWorkOrderNumberGenerator : IWorkOrderNumberGenerator
    {
        private int _counter;
        public string GenerateNumber() => $"A{++_counter:000000}";
    }

    private sealed class CountingDataContext(IDatabaseConfiguration config, StubCommandCounter counter) : DataContext(config)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.AddInterceptors(counter);
        }
    }

    private sealed class LoggingDataContext(IDatabaseConfiguration config, List<string> messages) : DataContext(config)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.LogTo(messages.Add, LogLevel.Information);
        }
    }

    private sealed class StubCommandCounter : DbCommandInterceptor
    {
        internal List<string> Commands { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Commands.Add(command.CommandText);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class BaselineContext(string path) : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseSqlite($"Data Source={path}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new WorkOrderMap().Map(modelBuilder);
            new EmployeeMap().Map(modelBuilder);
            new RoleMap().Map(modelBuilder);
            new WorkOrderAttachmentMap().Map(modelBuilder);
        }
    }
}
