using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Import;
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

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class WorkOrdersBulkImportControllerTests
{
    [Test]
    public async Task ShouldReturnBadRequest_WhenFileMissing()
    {
        var controller = CreateController(new StubBus());
        var result = await controller.Post(null!, CancellationToken.None);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public async Task ShouldReturnBadRequest_WhenFileEmpty()
    {
        var controller = CreateController(new StubBus());
        var file = CreateFormFile("", "empty.csv", "text/csv");
        file = new FormFile(new MemoryStream(), 0, 0, "file", "empty.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };

        var result = await controller.Post(file, CancellationToken.None);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public async Task ShouldReturnBadRequest_WhenNotCsv()
    {
        var controller = CreateController(new StubBus());
        var file = CreateFormFile("data", "notes.txt", "text/plain");

        var result = await controller.Post(file, CancellationToken.None);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public async Task ShouldReturnBadRequest_WhenCsvHasNoDataRows()
    {
        var controller = CreateController(new StubBus());
        var file = CreateFormFile("Title,Description,CreatorUsername\n", "headers-only.csv");

        var result = await controller.Post(file, CancellationToken.None);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public async Task ShouldReturnBadRequest_WhenCsvInvalid()
    {
        var controller = CreateController(new StubBus());
        var file = CreateFormFile("not,a,valid,header\nx,y,z\n", "bad.csv");

        var result = await controller.Post(file, CancellationToken.None);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(400);
    }

    [Test]
    public async Task ShouldReturnOk_WhenRowsImported()
    {
        var creator = new Employee("u1", "A", "B", "a@b.c");
        var bus = new StubBus { Employee = creator };
        var controller = CreateController(bus, new StubNumberGenerator { Next = "WO-001" });

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
    public async Task ShouldReturnRowFailure_WhenRequiredFieldsMissing()
    {
        var creator = new Employee("u1", "A", "B", "a@b.c");
        var bus = new StubBus { Employee = creator };
        var controller = CreateController(bus);

        var file = CreateFormFile("Title,Description,CreatorUsername\n,D1,u1\n");

        var result = await controller.Post(file, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<WorkOrderBulkImportResponse>();
        payload.CreatedCount.ShouldBe(0);
        payload.Results[0].Success.ShouldBeFalse();
        payload.Results[0].Error!.ShouldContain("required");
    }

    [Test]
    public async Task ShouldReturnRowFailure_WhenEmployeeNotFound()
    {
        var bus = new StubBus { Employee = null };
        var controller = CreateController(bus);

        var file = CreateFormFile("Title,Description,CreatorUsername\nT1,D1,nobody\n");

        var result = await controller.Post(file, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<WorkOrderBulkImportResponse>();
        payload.CreatedCount.ShouldBe(0);
        payload.Results[0].Error!.ShouldContain("nobody");
    }

    [Test]
    public async Task ShouldReturnRowFailure_WhenSaveDraftFails()
    {
        var creator = new Employee("u1", "A", "B", "a@b.c");
        var bus = new StubBus { Employee = creator, SaveDraftThrows = new InvalidOperationException("save failed") };
        var controller = CreateController(bus);

        var file = CreateFormFile("Title,Description,CreatorUsername\nT1,D1,u1\n");

        var result = await controller.Post(file, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<WorkOrderBulkImportResponse>();
        payload.CreatedCount.ShouldBe(0);
        payload.Results[0].Error.ShouldBe("save failed");
    }

    [Test]
    public async Task ShouldReuseCreatorLookup_WhenSameUsernameOnMultipleRows()
    {
        var creator = new Employee("u1", "A", "B", "a@b.c");
        var bus = new StubBus { Employee = creator };
        var controller = CreateController(bus, new StubNumberGenerator { Next = "WO-001" });

        var file = CreateFormFile("Title,Description,CreatorUsername\nT1,D1,u1\nT2,D2,u1\n");

        var result = await controller.Post(file, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<WorkOrderBulkImportResponse>();
        payload.CreatedCount.ShouldBe(2);
        bus.EmployeeLookupCalls.ShouldBe(1);
    }

    [Test]
    public void ShouldAcceptCsv_WhenExtensionOrContentTypeMatches()
    {
        WorkOrderBulkImportProcessor.IsCsvFile(CreateFormFile("x", "a.csv", "application/octet-stream")).ShouldBeTrue();
        WorkOrderBulkImportProcessor.IsCsvFile(CreateFormFile("x", "a.txt", "text/csv")).ShouldBeTrue();
        WorkOrderBulkImportProcessor.IsCsvFile(CreateFormFile("x", "a.txt", "application/vnd.ms-excel")).ShouldBeTrue();
        WorkOrderBulkImportProcessor.IsCsvFile(CreateFormFile("x", "a.txt", "text/plain")).ShouldBeFalse();
    }

    private static WorkOrdersBulkImportController CreateController(
        StubBus bus,
        StubNumberGenerator? numbers = null) =>
        new(bus, numbers ?? new StubNumberGenerator());

    private static IFormFile CreateFormFile(string csvContent, string fileName = "import.csv", string contentType = "text/csv")
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
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

        public Exception? SaveDraftThrows { get; set; }

        public int SaveDraftCalls { get; private set; }

        public int EmployeeLookupCalls { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeByUserNameQuery)
            {
                EmployeeLookupCalls++;
                if (Employee == null)
                {
                    throw new InvalidOperationException();
                }

                return Task.FromResult((TResponse)(object)Employee);
            }

            if (request is SaveDraftCommand cmd)
            {
                SaveDraftCalls++;
                if (SaveDraftThrows != null)
                {
                    throw SaveDraftThrows;
                }

                return Task.FromResult((TResponse)(object)new StateCommandResult(cmd.WorkOrder, "Save", "ok"));
            }

            throw new NotSupportedException(request.GetType().FullName);
        }

        public Task<object?> Send(object request) => throw new NotImplementedException();

        public Task Publish(INotification notification) => throw new NotImplementedException();
    }
}
