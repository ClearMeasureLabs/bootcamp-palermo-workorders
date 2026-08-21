using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using System.Collections.Concurrent;
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
    
    protected virtual bool? Headless { get; set; } = ServerFixture.HeadlessTestBrowser;
    protected virtual bool SkipScreenshotsForSpeed { get; set; } = ServerFixture.SkipScreenshotsForSpeed;
    public IBus Bus => TestHost.GetRequiredService<IBus>();

    protected static async Task SkipIfNoChatClient()
    {
        var factory = TestHost.GetRequiredService<ChatClientFactory>();
        var availability = await factory.IsChatClientAvailable();

        if (!availability.IsAvailable)
        {
            Assert.Ignore(availability.Message);
        }
    }

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

    private static readonly Random RandomPosition = new();

    protected virtual bool RequiresBrowser => true;

    [SetUp]
    public async Task SetUpAsync()
    {
        if (!RequiresBrowser) return;

        var testTag = Guid.NewGuid().ToString("N")[..8];
        var currentUser = CreateTestUser(testTag);

        var x = RandomPosition.Next(0, 1200);
        var y = RandomPosition.Next(0, 700);
        var browser = await ServerFixture.Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Headless,
            SlowMo = ServerFixture.SlowMo,
            Args = [$"--window-position={x},{y}", "--window-size=800,600"]
        });

        var browserContext = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = ServerFixture.ApplicationBaseUrl,
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 800, Height = 600 }
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

        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await page.GotoAsync("/");
                await page.WaitForURLAsync("/");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                break;
            }
            catch (PlaywrightException) when (attempt < maxRetries)
            {
                await Task.Delay(2000);
            }
        }
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        if (!TestStates.TryRemove(TestId, out var state))
            return;

        await SafeStopTracingAsync(state);
        await SafeCloseAsync(() => state.Page.CloseAsync(), "Page");
        await SafeCloseAsync(() => state.BrowserContext.CloseAsync(), "BrowserContext");
        await SafeCloseAsync(() => state.Browser.CloseAsync(), "Browser");
    }

    private async Task SafeStopTracingAsync(TestState state)
    {
        try
        {
            await state.BrowserContext.Tracing.StopAsync(new TracingStopOptions
            {
                Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "playwright-traces",
                    $"{TestContext.CurrentContext.Test.ClassName}.{TestContext.CurrentContext.Test.Name}.zip")
            });
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"TearDownAsync: ignoring tracing stop failure for {TestId}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task SafeCloseAsync(Func<Task> closeAsync, string resourceName)
    {
        try
        {
            await closeAsync();
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"TearDownAsync: ignoring failure closing {resourceName} for {TestId}: {ex.GetType().Name}: {ex.Message}");
        }
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

    /// <summary>
    /// Clicks an element by data-testid. Uses EvaluateAsync to trigger the click so Playwright does not
    /// wait for a full page navigation; Blazor WASM uses client-side routing, so navigation never
    /// "finishes" in the classic sense and would cause a 60s timeout. Callers that need to wait for a
    /// new view should use WaitForURLAsync or similar after Click.
    /// </summary>
    protected async Task Click(string elementTestId)
    {
        await TakeScreenshotAsync();
        var locator = Page.GetByTestId(elementTestId);
        await WaitForIfHiddenAsync(locator);
        await WaitForIfHiddenAsync(locator);
        await WaitForIfHiddenAsync(locator);
        await FocusIfVisibleAsync(locator);
        await BlurIfVisibleAsync(locator);
        await EvaluateClickIfVisibleAsync(locator);
    }

    /// <summary>
    /// Clicks a work-order state command submit button.
    /// Prefer Evaluate click (same as Click) so Blazor WASM client-side NavigateTo does not
    /// hang Playwright's default navigation wait. WorkOrderManage sets SelectedCommand on
    /// mousedown so EvaluateAsync(el.click()) still assigns the command before submit.
    /// </summary>
    protected async Task ClickCommandButton(string elementTestId)
    {
        await Click(elementTestId);
    }

    private static async Task WaitForIfHiddenAsync(ILocator locator)
    {
        if (!await locator.IsVisibleAsync())
        {
            await locator.WaitForAsync();
        }
    }

    private static async Task FocusIfVisibleAsync(ILocator locator)
    {
        if (await locator.IsVisibleAsync())
        {
            await locator.FocusAsync();
        }
    }

    private static async Task BlurIfVisibleAsync(ILocator locator)
    {
        if (await locator.IsVisibleAsync())
        {
            await locator.BlurAsync();
        }
    }

    private static async Task EvaluateClickIfVisibleAsync(ILocator locator)
    {
        if (!await locator.IsVisibleAsync())
        {
            return;
        }

        await locator.EvaluateAsync("el => el.click()");
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
        var testInstructions = order.Instructions;
        var testRoomNumber = order.RoomNumber;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await TakeScreenshotAsync(1, "NewWorkOrderPage");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();
        order.Number = newWorkOrderNumber;
        await Input(nameof(WorkOrderManage.Elements.Title), testTitle);
        await Input(nameof(WorkOrderManage.Elements.Description), testDescription);
        if (!string.IsNullOrEmpty(testInstructions))
        {
            await Input(nameof(WorkOrderManage.Elements.Instructions), testInstructions);
        }
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), testRoomNumber);
        await TakeScreenshotAsync(2, "FormFilled");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehyratedOrder = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
            if (rehyratedOrder != null) break;
            await Task.Delay(1000);
        }
        rehyratedOrder.ShouldNotBeNull();

        return rehyratedOrder;
    }

    protected async Task<WorkOrder> ClickWorkOrderNumberFromSearchPage(WorkOrder order)
    {
        // Status commands NavigateTo /workorder/search; wait for that URL and row link
        // before click so Blazor result render races do not time out on WorkOrderLink*.
        if (!Page.Url.Contains("/workorder/search", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 60_000 });
            }
            catch (TimeoutException)
            {
                if (!Page.Url.Contains("/workorder/search", StringComparison.OrdinalIgnoreCase))
                {
                    throw;
                }
            }
        }

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var linkTestId = nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number;
        await Page.GetByTestId(linkTestId).WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });
        await Click(linkTestId);
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

        var assignButtonTestId =
            nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name;
        await Expect(Page.GetByTestId(assignButtonTestId))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });

        var searchNav = Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 30_000 });
        await ClickCommandButton(assignButtonTestId);
        try
        {
            await searchNav;
        }
        catch (TimeoutException)
        {
        }

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehyratedOrder = null;
        for (var attempt = 0; attempt < 40; attempt++)
        {
            rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!))
                ?? throw new InvalidOperationException();
            if (rehyratedOrder.Status == WorkOrderStatus.Assigned && rehyratedOrder.Assignee != null)
            {
                return rehyratedOrder;
            }

            await Task.Delay(250);
        }

        rehyratedOrder!.Status.ShouldBe(WorkOrderStatus.Assigned);
        rehyratedOrder.Assignee.ShouldNotBeNull();
        return rehyratedOrder;
    }

    protected async Task<WorkOrder> BeginExistingWorkOrder(WorkOrder order)
    {
        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        var beginButtonTestId =
            nameof(WorkOrderManage.Elements.CommandButton) + AssignedToInProgressCommand.Name;
        var searchNav = Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 30_000 });
        await ClickCommandButton(beginButtonTestId);
        try
        {
            await searchNav;
        }
        catch (TimeoutException)
        {
        }

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehyratedOrder = null;
        for (var attempt = 0; attempt < 40; attempt++)
        {
            rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!))
                ?? throw new InvalidOperationException();
            if (rehyratedOrder.Status == WorkOrderStatus.InProgress)
            {
                return rehyratedOrder;
            }

            await Task.Delay(250);
        }

        rehyratedOrder!.Status.ShouldBe(WorkOrderStatus.InProgress);
        return rehyratedOrder;
    }

    protected async Task<WorkOrder> CompleteExistingWorkOrder(WorkOrder order)
    {
        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        if (!string.IsNullOrEmpty(order.Instructions))
        {
            await Input(nameof(WorkOrderManage.Elements.Instructions), order.Instructions);
        }

        var completeButtonTestId =
            nameof(WorkOrderManage.Elements.CommandButton) + InProgressToCompleteCommand.Name;
        // Soft timeout: ARM runners sometimes miss SPA nav; poll Bus + later search wait recover.
        var searchNav = Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 30_000 });
        await ClickCommandButton(completeButtonTestId);
        try
        {
            await searchNav;
        }
        catch (TimeoutException)
        {
            // Submit may still be in flight; ClickWorkOrderNumberFromSearchPage waits for search later.
        }

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehyratedOrder = null;
        for (var attempt = 0; attempt < 40; attempt++)
        {
            rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!))
                ?? throw new InvalidOperationException();
            if (rehyratedOrder.Status == WorkOrderStatus.Complete && rehyratedOrder.CompletedDate != null)
            {
                return rehyratedOrder;
            }

            await Task.Delay(250);
        }

        rehyratedOrder!.Status.ShouldBe(WorkOrderStatus.Complete);
        rehyratedOrder.CompletedDate.ShouldNotBeNull();
        return rehyratedOrder;
    }
}