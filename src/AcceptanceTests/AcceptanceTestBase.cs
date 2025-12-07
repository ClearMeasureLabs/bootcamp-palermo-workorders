using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using System.Globalization;
using Login = ClearMeasure.Bootcamp.UI.Shared.Pages.Login;

namespace ClearMeasure.Bootcamp.AcceptanceTests;

public abstract class AcceptanceTestBase : PageTest
{
    public Employee CurrentUser { get; set; } = null!;
    protected virtual bool? Headless { get; set; } = true;
    protected virtual bool LoadDataOnSetup { get; set; } = true;
    protected virtual bool SkipScreenshotsForSpeed { get; set; } = ServerFixture.SkipScreenshotsForSpeed;
    protected new IPage Page { get; private set; }
    public IBus Bus => TestHost.GetRequiredService<IBus>();

    [SetUp]
    public async Task SetUpAsync()
    {
        if (LoadDataOnSetup)
        {
            new ZDataLoader().LoadData();
            CurrentUser = new ZDataLoader().CreateUser();
        }

        await Context.Tracing.StartAsync(new TracingStartOptions
        {
            Title = $"{TestContext.CurrentContext.Test.ClassName}.{TestContext.CurrentContext.Test.Name}",
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });

        var playwright = Playwright;
        var browser = await GetBrowserTypeInstance(playwright).LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Headless,
            SlowMo = ServerFixture.SlowMo//milliseconds delay to thwart race conditions (slower computer needs higher number)
        });

        var context = await browser.NewContextAsync(ContextOptions());
        context.SetDefaultTimeout(60_000);
        Page = await context.NewPageAsync().ConfigureAwait(false);
        await Page.GotoAsync("/");
        await Page.WaitForURLAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    protected virtual IBrowserType GetBrowserTypeInstance(IPlaywright playwright)
    {
        return playwright.Chromium;
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        await Context.Tracing.StopAsync(new TracingStopOptions
        {
            Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "playwright-traces",
                $"{TestContext.CurrentContext.Test.ClassName}.{TestContext.CurrentContext.Test.Name}.zip")
        });

        await Page.CloseAsync();
    }

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            BaseURL = ServerFixture.ApplicationBaseUrl,
            IgnoreHTTPSErrors = true
        };
    }

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
        await locator.ClearAsync();
        await locator.FillAsync(value ?? "");
        await locator.BlurAsync();
        
        var delayMs = GetInputDelayMs();
        await Task.Delay(delayMs);
        
        await Expect(locator).ToHaveValueAsync(value ?? "");
    }

    private int GetInputDelayMs()
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
        await locator.SelectOptionAsync(value ?? "");
    }

    protected async Task<WorkOrder> CreateAndSaveNewWorkOrder()
    {
        var order = Faker<WorkOrder>();
        order.Title = "from automation";
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