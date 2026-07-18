using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.IntegrationTests;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpServerLlmAcceptanceTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    private static McpTestHelper? _helper;

    [OneTimeSetUp]
    public async Task McpSetUp()
    {
        _helper = new McpTestHelper(TestHost.GetRequiredService<ChatClientFactory>());
        await _helper.ConnectAsync();
    }

    [OneTimeTearDown]
    public async Task McpTearDown()
    {
        if (_helper != null) await _helper.DisposeAsync();
    }

    [SetUp]
    public async Task EnsureAvailability()
    {
        if (!_helper!.Connected)
            Assert.Inconclusive("MCP server is not available");
        await SkipIfNoChatClient();
    }

    [Test, Retry(2)]
    public async Task ShouldListWorkRequestsViaLlm()
    {
        var response = await _helper!.SendPrompt(
            "Use the list-work-requests tool to list all work requests in the system. " +
            "Return the work request numbers you find.");

        response.Text.ShouldNotBeNullOrEmpty();
    }

    [Test, Retry(2)]
    public async Task ShouldGetWorkRequestByNumberViaLlm()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var workRequests = await bus.Send(new WorkRequestSpecificationQuery());
        var knownOrder = workRequests.First();

        var response = await _helper!.SendPrompt(
            $"Use the get-work-request tool to get the details of work request number '{knownOrder.Number}'. " +
            "Return the title and status.");

        response.Text.ShouldNotBeNullOrEmpty();
        response.Text.ShouldContain(knownOrder.Title!);
    }

    [Test, Retry(2)]
    public async Task ShouldCreateWorkRequestViaLlm()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var employees = await bus.Send(new EmployeeGetAllQuery());
        var creator = employees.First(e => e.Roles.Any(r => r.CanCreateWorkRequest));

        var response = await _helper!.SendPrompt(
            $"Call the create-work-request tool with these exact parameters: " +
            $"title='Repair sanctuary roof', description='Roof tiles need replacement', " +
            $"creatorUsername='{creator.UserName}'.");

        response.Text.ShouldNotBeNullOrEmpty();
        var responseText = response.Text.ToLowerInvariant();
        (responseText.Contains("repair") || responseText.Contains("draft") || responseText.Contains("created") || responseText.Contains("wo-"))
            .ShouldBeTrue($"Expected creation confirmation in response: {response.Text}");
    }

    [Test, Retry(2)]
    public async Task ShouldListEmployeesViaLlm()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var employees = await bus.Send(new EmployeeGetAllQuery());
        var knownUsernames = employees.Select(e => e.UserName).ToList();

        var response = await _helper!.SendPrompt(
            "Use the list-employees tool to list all employees in the system. " +
            "Return their usernames.");

        response.Text.ShouldNotBeNullOrEmpty();
        knownUsernames.ShouldContain(
            username => response.Text.Contains(username!),
            "Response should contain at least one known employee username");
    }
}
