using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using System.Collections.Concurrent;
using System.Globalization;
using Login = ClearMeasure.Bootcamp.UI.Shared.Pages.Login;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Per-test state container for parallel test execution.
/// Each test gets its own isolated state including browser page, user, and test tag.
/// </summary>
public class TestState
{
    public required IPage Page { get; init; }
    public required IBrowserContext BrowserContext { get; init; }
    public required IBrowser Browser { get; init; }
    public Employee CurrentUser { get; set; } = null!;
    public required string TestTag { get; init; }
}

/// <summary>
/// Base class for acceptance tests supporting parallel execution with ParallelScope.Children.
/// Each test gets its own browser, page, user, and test tag for complete isolation.
/// Does not inherit from PageTest to avoid thread-safety issues with Playwright's internal collections.
/// </summary>
public abstract class AcceptanceTestBase
{
    private static readonly ConcurrentDictionary<string, TestState> TestStates = new();
    
    protected virtual bool? Headless { get; set; } = true;
    protected virtual bool SkipScreenshotsForSpeed { get; set; } = ServerFixture.SkipScreenshotsForSpeed;
    public IBus Bus => TestHost.GetRequiredService<IBus>();

    private string TestId => TestContext.CurrentContext.Test.ID;
    
    /// <summary>
    /// Gets the current test's state container.
    /// </summary>
    private TestState State => TestStates[TestId];
    
    /// <summary>
    /// Gets the browser page for the current test.
    /// </summary>
    protected IPage Page => State.Page;
    
    /// <summary>
    /// Gets or sets the current user for the current test.
    /// </summary>
    public Employee CurrentUser
    {
        get => State.CurrentUser;
        set => State.CurrentUser = value;
    }
    
    /// <summary>
    /// Unique tag for this test instance to isolate test data in parallel execution.
    /// </summary>
    protected string TestTag => State.TestTag;

    [SetUp]
    public async Task SetUpAsync()
    {
        var testTag = Guid.NewGuid().ToString("N")[..8];
        var currentUser = CreateTestUser(testTag);

        var browser = await ServerFixture.Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Headless,
            SlowMo = ServerFixture.SlowMo
        });

        var browserContext = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = ServerFixture.ApplicationBaseUrl,
            IgnoreHTTPSErrors = true
        });
        browserContext.SetDefaultTimeout(60_000);
        
        await browserContext.Tracing.StartAsync(new TracingStartOptions
        {
            Title = $"{TestContext.CurrentContext.Test.ClassName}.{TestContext.CurrentContext.Test.Name}",
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });

        var page = await browserContext.NewPageAsync().ConfigureAwait(false);
        
        var state = new TestState
        {
            Page = page,
            BrowserContext = browserContext,
            Browser = browser,
            CurrentUser = currentUser,
            TestTag = testTag
        };
        
        TestStates[TestId] = state;

        await page.GotoAsync("/");
        await page.WaitForURLAsync("/");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        if (!TestStates.TryRemove(TestId, out var state))
            return;

        try
        {
            await state.BrowserContext.Tracing.StopAsync(new TracingStopOptions
            {
                Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "playwright-traces",
                    $"{TestContext.CurrentContext.Test.ClassName}.{TestContext.CurrentContext.Test.Name}.zip")
            });
        }
        catch
        {
            // Ignore tracing errors during teardown
        }

        try { await state.Page.CloseAsync(); } catch { }
        try { await state.BrowserContext.CloseAsync(); } catch { }
        try { await state.Browser.CloseAsync(); } catch { }
    }

    /// <summary>
    /// Playwright assertion helper - wraps Assertions.Expect for the current page context.
    /// </summary>
    protected ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);
    
    /// <summary>
    /// Playwright assertion helper - wraps Assertions.Expect for the current page.
    /// </summary>
    protected IPageAssertions Expect(IPage page) => Assertions.Expect(page);

    protected async Task TakeScreenshotAsync(int stepNumber=0, string? stepName = null)
    {
        if (SkipScreenshotsForSpeed) return;

        var test = TestContext.CurrentContext.Test;
        var testName = test.ClassName + "-" + test.Name;
        var fileName = $"{testName}-{stepNumber}{stepName}{Guid.NewGuid()}.png";
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = fileName
        });
        TestContext.AddTestAttachment(Path.GetFullPath(fileName));
    }

    protected TK Faker<TK>()
    {
        return TestHost.Faker<TK>();
    }

    /// <summary>
    /// Creates a unique test user for this test instance to enable parallel test execution.
    /// </summary>
    private static Employee CreateTestUser(string testTag)
    {
        using var context = TestHost.NewDbContext();
        var employee = TestHost.Faker<Employee>();
        employee.UserName = $"test_{testTag}_{employee.UserName}";
        employee.AddRole(new Role("admin", true, true));
        context.Add(employee);
        context.SaveChanges();
        return employee;
    }

    protected async Task LoginAsCurrentUser()
    {
        var username = CurrentUser.UserName;
        await TakeScreenshotAsync();
        await Click(nameof(LoginLink.Elements.LoginLink));
        await Page.WaitForURLAsync("**/login");
        await Expect(Page.GetByTestId(nameof(Login.Elements.User))).ToBeVisibleAsync();

        // Fill in username only
        await Select(nameof(Login.Elements.User), username);

        // Submit form
        await TakeScreenshotAsync();
        await Click(nameof(Login.Elements.LoginButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await TakeScreenshotAsync();
        // Assert: Should be redirected to home and see welcome message
        var welcomeTextLocator = Page.GetByTestId(nameof(Logout.Elements.WelcomeText));
        await Expect(welcomeTextLocator).ToContainTextAsync($"Welcome {CurrentUser.UserName}");
        await welcomeTextLocator.DblClickAsync(); // causes the browser to finish DOM loading - HACK
    }

    protected async Task Click(string elementTestId)
    {
        await TakeScreenshotAsync();
        ILocator locator = Page.GetByTestId(elementTestId);
        if(!await locator.IsVisibleAsync()) await locator.WaitForAsync();
        if (!await locator.IsVisibleAsync()) await locator.WaitForAsync();
        if (!await locator.IsVisibleAsync()) await locator.WaitForAsync();
        if (await locator.IsVisibleAsync()) await locator.FocusAsync();
        if (await locator.IsVisibleAsync()) await locator.BlurAsync();
        if (await locator.IsVisibleAsync()) await locator.ClickAsync();
    }

    protected async Task Input(string elementTestId, string? value)
    {
        var locator = Page.GetByTestId(elementTestId);
        if (!await locator.IsVisibleAsync()) await locator.WaitForAsync();
        if (!await locator.IsVisibleAsync()) await locator.WaitForAsync();
        if (!await locator.IsVisibleAsync()) await locator.WaitForAsync();
        await Expect(locator).ToBeVisibleAsync();

        // Under parallel load, the Blazor WASM page may render with disabled fields
        // before the server responds with state commands. Retry with a page reload
        // if the field is not editable within a short window.
        if (!await locator.IsEditableAsync())
        {
            await Page.ReloadAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await Expect(locator).ToBeEditableAsync(new LocatorAssertionsToBeEditableOptions { Timeout = 30_000 });
        await locator.ClearAsync();
        await locator.FillAsync(value ?? "");
        await locator.BlurAsync();

        var delayMs = GetInputDelayMs();
        await Task.Delay(delayMs);

        await Expect(locator).ToHaveValueAsync(value ?? "");
    }

    protected int GetInputDelayMs()
    {
        var envValue = Environment.GetEnvironmentVariable("TEST_INPUT_DELAY_MS");
        if (int.TryParse(envValue, out var delay))
        {
            return delay;
        }
        return 100; // Default to 100ms for local performance
    }

    protected async Task Select(string elementTestId, string? value)
    {
        var locator = Page.GetByTestId(elementTestId);
        await Expect(locator).ToBeVisibleAsync();

        if (!await locator.IsEditableAsync())
        {
            await Page.ReloadAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        await Expect(locator).ToBeEditableAsync(new LocatorAssertionsToBeEditableOptions { Timeout = 30_000 });
        await locator.SelectOptionAsync(value ?? "");
    }

    protected async Task<WorkOrder> CreateAndSaveNewWorkOrder()
    {
        var order = Faker<WorkOrder>();
        order.Title = $"[{TestTag}] from automation";
        order.Number = null;
        var testTitle = order.Title;
        var testDescription = order.Description;
        var testRoomNumber = order.RoomNumber;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;
        await Input(nameof(WorkOrderManage.Elements.Title), testTitle);
        await Input(nameof(WorkOrderManage.Elements.Description), testDescription);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);
        await TakeScreenshotAsync(2, "FormFilled");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        if (rehyratedOrder == null)
        {
            await Task.Delay(1000); 
            rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
        }
        rehyratedOrder.ShouldNotBeNull();

        return rehyratedOrder;
    }

    protected async Task<WorkOrder> ClickWorkOrderNumberFromSearchPage(WorkOrder order)
    {
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        return order;
    }

    protected async Task<WorkOrder> AssignExistingWorkOrder(WorkOrder order, string username)
    {
        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);
        
        await Select(nameof(WorkOrderManage.Elements.Assignee), username);
        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();

        return rehyratedOrder;
    }

    protected async Task<WorkOrder> BeginExistingWorkOrder(WorkOrder order)
    {
        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + AssignedToInProgressCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();

        return rehyratedOrder;
    }

    protected async Task<WorkOrder> CompleteExistingWorkOrder(WorkOrder order)
    {
        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + InProgressToCompleteCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Task.Delay(GetInputDelayMs()); // Give time for the save operation to complete on Azure
        WorkOrder rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        return rehyratedOrder;
    }
}