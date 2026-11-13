using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests;

[TestFixture]
[Category("SqlServerOnly")]
public class AuditLogTests
{
    [Test]
    public async Task ShouldInsertAuditWithSqlTriggersAndPurgeOnlyOlderRecords()
    {
        SqlServerTestAssumptions.RequireSqlServer();
        await using var context = TestHost.GetRequiredService<DbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var entityId = Guid.NewGuid();
        var cutoff = DateTime.UtcNow.AddYears(-20);
        context.AddRange(
            new AuditLog
            {
                Action = "Old",
                EntityType = "RetentionTest",
                EntityId = entityId,
                Timestamp = cutoff.AddSeconds(-1)
            },
            new AuditLog
            {
                Action = "Boundary",
                EntityType = "RetentionTest",
                EntityId = entityId,
                Timestamp = cutoff
            });
        await context.SaveChangesAsync();
        var service = new AuditLogService(context);

        (await service.PurgeAsync(cutoff)).ShouldBeGreaterThanOrEqualTo(1);

        var remaining = await service.ReadAsync("RetentionTest", entityId, null, null);
        remaining.Length.ShouldBe(1);
        remaining[0].Action.ShouldBe("Boundary");
        remaining[0].Timestamp.Kind.ShouldBe(DateTimeKind.Utc);
        JsonSerializer.Serialize(remaining[0].Timestamp).ShouldEndWith("Z\"");
        await transaction.RollbackAsync();
    }

    [Test]
    public async Task ShouldRejectDirectSqlAuditUpdates()
    {
        SqlServerTestAssumptions.RequireSqlServer();
        await using var context = TestHost.GetRequiredService<DbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var entityId = Guid.NewGuid();
        await new AuditLogService(context).WriteAsync("Viewed", "TamperingTest", entityId);

        await Should.ThrowAsync<SqlException>(() => context.Database.ExecuteSqlRawAsync(
            "UPDATE dbo.AuditLog SET Action = N'Tampered' WHERE EntityId = {0}", entityId));
    }

    [Test]
    public async Task ShouldAllowPrivilegedSqlDeleteForRetentionAndDatabaseReset()
    {
        SqlServerTestAssumptions.RequireSqlServer();
        await using var context = TestHost.GetRequiredService<DbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var entityId = Guid.NewGuid();
        await new AuditLogService(context).WriteAsync("Viewed", "ResetTest", entityId);

        var deleted = await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM dbo.AuditLog WHERE EntityId = {0}", entityId);

        deleted.ShouldBe(1);
        (await context.Set<AuditLog>().AsNoTracking().AnyAsync(log => log.EntityId == entityId)).ShouldBeFalse();
        await transaction.RollbackAsync();
    }

    [Test]
    public async Task ShouldCleanSqlAuditHistoryWithinExplicitRetentionScope()
    {
        SqlServerTestAssumptions.RequireSqlServer();
        await using var context = TestHost.GetRequiredService<DbContext>();
        var cleanup = new DatabaseEmptier(context.Database);
        cleanup.DeleteAllData();
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER dbo.AuditCleanupRetentionGuard ON dbo.AuditLog AFTER DELETE
            AS
            BEGIN
                SET NOCOUNT ON;
                IF SESSION_CONTEXT(N'AuditRetentionCutoff') IS NULL
                    THROW 51001, 'Audit cleanup requires a retention cutoff.', 1;
            END;
            """);
        var employee = new Employee($"cleanup-{Guid.NewGuid():N}", "First", "Last", "cleanup@example.test");
        context.Add(employee);
        context.Add(new AuditLog
        {
            Action = "CleanupTest",
            EntityType = nameof(Employee),
            EntityId = employee.Id,
            Timestamp = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        cleanup.DeleteAllData();

        (await context.Set<Employee>().AnyAsync(person => person.Id == employee.Id)).ShouldBeFalse();
        (await context.Set<AuditLog>().AsNoTracking().AnyAsync(log => log.EntityId == employee.Id)).ShouldBeFalse();
        var marker = await context.Database.SqlQueryRaw<int>(
            "SELECT CASE WHEN SESSION_CONTEXT(N'AuditRetentionCutoff') IS NULL THEN 0 ELSE 1 END AS [Value]")
            .SingleAsync();
        marker.ShouldBe(0);
        await transaction.RollbackAsync();
    }

    [Test]
    public async Task ShouldRollbackSqlBusinessAndAuditWritesTogether()
    {
        SqlServerTestAssumptions.RequireSqlServer();
        var username = $"audit-{Guid.NewGuid():N}";
        Guid employeeId;
        await using (var context = TestHost.GetRequiredService<DbContext>())
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            using var scope = AuditContext.Begin(new AuditContext("integration-actor", CorrelationId: "rollback-test"));
            var employee = new Employee(username, "First", "Last", "audit@example.test");
            context.Add(employee);
            await context.SaveChangesAsync();
            employeeId = employee.Id;
            (await context.Set<AuditLog>().SingleAsync(log =>
                log.EntityType == nameof(Employee) && log.EntityId == employeeId)).ActorUserName
                .ShouldBe("integration-actor");
            await transaction.RollbackAsync();
        }

        await using var verify = TestHost.GetRequiredService<DbContext>();
        (await verify.Set<Employee>().AnyAsync(employee => employee.UserName == username)).ShouldBeFalse();
        (await verify.Set<AuditLog>().AnyAsync(log =>
            log.EntityType == nameof(Employee) && log.EntityId == employeeId)).ShouldBeFalse();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ShouldNotReplaySqlHistoryWhenAcceptanceIsDeferred(bool asynchronous)
    {
        SqlServerTestAssumptions.RequireSqlServer();
        await using var context = TestHost.GetRequiredService<DbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var employee = new Employee($"audit-{Guid.NewGuid():N}", "Original", "Last", "audit@example.test");
        context.Add(employee);
        await context.SaveChangesAsync();
        employee.FirstName = "First update";
        if (asynchronous)
            await context.SaveChangesAsync(false);
        else
            context.SaveChanges(false);
        employee.FirstName = "Second update";

        if (asynchronous)
            await context.SaveChangesAsync();
        else
            context.SaveChanges();

        var records = await context.Set<AuditLog>().Where(log =>
            log.EntityType == nameof(Employee) && log.EntityId == employee.Id && log.Action == "Modified")
            .OrderBy(log => log.Id).ToArrayAsync();
        records.Length.ShouldBe(2);
        JsonSerializer.Deserialize<JsonElement>(records[1].PreviousValue!)
            .GetProperty("FirstName").GetString().ShouldBe("First update");
        JsonSerializer.Deserialize<JsonElement>(records[1].NewValue!)
            .GetProperty("FirstName").GetString().ShouldBe("Second update");
        await transaction.RollbackAsync();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ShouldAuditSqlDetachedWorkOrderUpdatesUsingPersistedOriginals(bool asynchronous)
    {
        SqlServerTestAssumptions.RequireSqlServer();
        await using var context = TestHost.GetRequiredService<DbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var employee = new Employee($"audit-{Guid.NewGuid():N}", "First", "Last", "audit@example.test");
        var order = new WorkOrder { Number = "AUDIT1", Title = "Original", Creator = employee };
        context.Add(order);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        order.Title = "Detached update";
        context.Attach(order);
        context.Update(order);
        using var scope = AuditContext.Begin(new AuditContext("system", Action: "DetachedUpdate"));

        if (asynchronous)
            await context.SaveChangesAsync();
        else
            context.SaveChanges();

        var record = await context.Set<AuditLog>().SingleAsync(log =>
            log.EntityType == nameof(WorkOrder) && log.EntityId == order.Id && log.Action == "DetachedUpdate");
        JsonSerializer.Deserialize<JsonElement>(record.PreviousValue!)
            .GetProperty("Title").GetString().ShouldBe("Original");
        JsonSerializer.Deserialize<JsonElement>(record.NewValue!)
            .GetProperty("Title").GetString().ShouldBe("Detached update");
        (await context.Set<AuditLog>().CountAsync(log =>
            log.EntityType == nameof(Employee) && log.EntityId == employee.Id && log.Action == "DetachedUpdate"))
            .ShouldBe(0);
        await transaction.RollbackAsync();
    }
}
