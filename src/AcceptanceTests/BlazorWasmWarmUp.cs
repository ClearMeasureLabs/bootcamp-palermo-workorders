namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Warms up the Blazor WebAssembly application by loading the home page in a headless browser,
/// detecting JavaScript errors or stuck loading screens, and reloading until the LoginLink is visible.
/// This ensures the WASM payload is fully downloaded and initialized before parallel tests begin.
/// </summary>
public class BlazorWasmWarmUp
{
    private const int MaxAttempts = 6;
    private const int TimeoutSeconds = 60;

    private readonly IPlaywright _playwright;
    private readonly string _baseUrl;

    public BlazorWasmWarmUp(IPlaywright playwright, string baseUrl)
    {
        _playwright = playwright;
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Loads the home page in a disposable headless browser, retrying on JavaScript errors
    /// or stuck loading screens until the LoginLink element is visible.
    /// </summary>
    public async Task ExecuteAsync()
    {
        TestContext.Out.WriteLine("Blazor WASM warm-up: starting...");

        await using var browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = _baseUrl,
            IgnoreHTTPSErrors = true
        });
        context.SetDefaultTimeout(TimeoutSeconds * 1000);

        var page = await context.NewPageAsync();

        var jsErrors = new List<string>();
        page.PageError += (_, error) => jsErrors.Add(error);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            jsErrors.Clear();
            TestContext.Out.WriteLine($"Blazor WASM warm-up: attempt {attempt}/{MaxAttempts}");

            try
            {
                await page.GotoAsync("/", new PageGotoOptions { Timeout = TimeoutSeconds * 1000 });
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                    new PageWaitForLoadStateOptions { Timeout = TimeoutSeconds * 1000 });

                var loginLink = page.GetByTestId("LoginLink");
                await loginLink.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = TimeoutSeconds * 1000
                });

                TestContext.Out.WriteLine("Blazor WASM warm-up: LoginLink visible — app is ready.");
                await page.CloseAsync();
                await context.CloseAsync();
                return;
            }
            catch (TimeoutException)
            {
                TestContext.Out.WriteLine(
                    $"Blazor WASM warm-up: timeout (navigation, network idle, or LoginLink) after {TimeoutSeconds}s. " +
                    $"JS errors captured: {jsErrors.Count}");
                foreach (var error in jsErrors)
                {
                    TestContext.Out.WriteLine($"  JS error: {error}");
                }

                if (attempt < MaxAttempts)
                {
                    TestContext.Out.WriteLine("Blazor WASM warm-up: reloading page...");
                    try
                    {
                        await page.ReloadAsync(new PageReloadOptions { Timeout = TimeoutSeconds * 1000 });
                        await page.WaitForLoadStateAsync(LoadState.NetworkIdle,
                            new PageWaitForLoadStateOptions { Timeout = TimeoutSeconds * 1000 });
                    }
                    catch (TimeoutException)
                    {
                        TestContext.Out.WriteLine("Blazor WASM warm-up: reload timed out; will retry from root on next attempt.");
                    }
                }
            }
        }

        await page.CloseAsync();
        await context.CloseAsync();
        TestContext.Out.WriteLine(
            $"WARNING: Blazor WASM warm-up did not confirm LoginLink after {MaxAttempts} attempts. " +
            "Tests will proceed but may encounter loading issues.");
    }
}
