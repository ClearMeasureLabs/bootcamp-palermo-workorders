using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using System.Text;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class WorkOrdersBulkImportControllerTests
{
    [Test]
    public async Task ShouldReturnBadRequest_WhenFileMissing()
    {
        var controller = new WorkOrdersBulkImportController(new StubBus(), new StubNumberGenerator());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        var result = await controller.Post(null, CancellationToken.None);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public async Task ShouldReturnOk_WhenRowsImported()
    {
        var creator = new Employee("u1", "A", "B", "a@b.c");
        var bus = new StubBus { Employee = creator };
        var controller = new WorkOrdersBulkImportController(bus, new StubNumberGenerator { Next = "WO-001" });

        var file = CreateFormFile("Title,Description,CreatorUsername\nT1,D1,u1\n");

        var result = await controller.Post(file, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<WorkOrderBulkImportResponse>();
        payload.CreatedCount.ShouldBe(1);
        payload.Results.Count.ShouldBe(1);
        payload.Results[0].Success.ShouldBeTrue();
        payload.Results[0].WorkOrderNumber.ShouldBe("WO-001");
        bus.SaveDraftCalls.ShouldBe(1);
    }

    [Test]
    public async Task ShouldReturnOk_WhenCsvPostedAsUrlEncodedForm()
    {
        var creator = new Employee("u1", "A", "B", "a@b.c");
        var bus = new StubBus { Employee = creator };
        var controller = new WorkOrdersBulkImportController(bus, new StubNumberGenerator { Next = "WO-002" });

        var csv = "Title,Description,CreatorUsername,Instructions\nT1,D1,u1,Bring ladder\n";
        var body = "csv=" + Uri.EscapeDataString(csv);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/x-www-form-urlencoded; charset=utf-8";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        var result = await controller.Post(null, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<WorkOrderBulkImportResponse>();
        payload.CreatedCount.ShouldBe(1);
        bus.SaveDraftCalls.ShouldBe(1);
        bus.LastSavedInstructions.ShouldBe("Bring ladder");
    }

    private static IFormFile CreateFormFile(string csvContent)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", "import.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };
    }

    private sealed class StubNumberGenerator : IWorkOrderNumberGenerator
    {
        public string Next { get; set; } = "N1";

        public string GenerateNumber() => Next;
    }

    private sealed class StubBus : IBus
    {
        public Employee? Employee { get; set; }

        public int SaveDraftCalls { get; private set; }

        public string? LastSavedInstructions { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeByUserNameQuery)
            {
                if (Employee == null)
                {
                    throw new InvalidOperationException();
                }

                return Task.FromResult((TResponse)(object)Employee);
            }

            if (request is SaveDraftCommand cmd)
            {
                SaveDraftCalls++;
                LastSavedInstructions = cmd.WorkOrder.Instructions;
                return Task.FromResult((TResponse)(object)new StateCommandResult(cmd.WorkOrder, "Save", "ok"));
            }

            throw new NotSupportedException(request?.GetType().FullName);
        }

        public Task<object?> Send(object request) => throw new NotImplementedException();

        public Task Publish(INotification notification) => throw new NotImplementedException();
    }
}
