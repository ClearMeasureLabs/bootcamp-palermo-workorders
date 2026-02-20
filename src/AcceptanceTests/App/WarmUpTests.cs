namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>
/// Verifies the Blazor WebAssembly application loaded successfully.
/// The actual warm-up is performed in <see cref="ServerFixture.OneTimeSetUp"/>
/// via <see cref="BlazorWasmWarmUp"/> before any test fixture runs.
/// </summary>
[TestFixture]
public class WarmUpTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task WarmUp_BlazorWasm_LoginLinkVisible()
    {
        var loginLink = Page.GetByTestId("LoginLink");
        await Expect(loginLink).ToBeVisibleAsync();
    }
}
