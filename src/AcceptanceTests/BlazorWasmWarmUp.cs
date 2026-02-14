namespace ClearMeasure.Bootcamp.AcceptanceTests;

/// <summary>
/// Warms up the Blazor WebAssembly application by loading the home page in a headless browser,
/// detecting JavaScript errors or stuck loading screens, and reloading until the LoginLink is visible.
/// This ensures the WASM payload is fully downloaded and initialized before parallel tests begin.
/// </summary>
public class BlazorWasmWarmUp
{
    private const int MaxAttempts = 5;
    private const int TimeoutSeconds = 30;

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

            await page.GotoAsync("/");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var loginLink = page.GetByTestId("LoginLink");
            try
            {
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
                    $"Blazor WASM warm-up: LoginLink not visible after {TimeoutSeconds}s. " +
                    $"JS errors captured: {jsErrors.Count}");
                foreach (var error in jsErrors)
                {
                    TestContext.Out.WriteLine($"  JS error: {error}");
                }

                if (attempt < MaxAttempts)
                {
                    TestContext.Out.WriteLine("Blazor WASM warm-up: reloading page...");
                    await page.ReloadAsync();
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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
