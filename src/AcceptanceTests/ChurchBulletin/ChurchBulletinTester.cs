using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace ProgrammingWithPalermo.ChurchBulletin.AcceptanceTests.ChurchBulletin
{
    [Parallelizable(ParallelScope.Self)]
    [TestFixture]
    public class ChurchBulletinTester : PageTest
    {

        [SetUp]
        public async Task SetUpAsync()
        {
            await Context.Tracing.StartAsync(new()
            {
                Title = $"{TestContext.CurrentContext.Test.ClassName}.{TestContext.CurrentContext.Test.Name}",
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });
        }

        [Test]
        public async Task ShouldLoadChurchBulletin()
        {
            // Arrange
            await Page.GotoAsync($"/fetchchurchbulletin");

            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await TakeScreenshotAsync(10, TestContext.CurrentContext.Test.Name, "Arrange");

            // Act


            await TakeScreenshotAsync(20, TestContext.CurrentContext.Test.Name, "Act");

            await Page.WaitForTimeoutAsync(10000);

            // Assert

            await TakeScreenshotAsync(30, TestContext.CurrentContext.Test.Name, "Assert");

            await Expect(Page.Locator("h1")).ToContainTextAsync("Church Bulletin", new() { Timeout = 10000 });

            //await Expect(totalCount).ToContainTextAsync($"{expectedCount}");
        }

        [TearDown]
        public async Task TearDownAsync()
        {
            await Context.Tracing.StopAsync(new()
            {
                Path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "playwright-traces", $"{TestContext.CurrentContext.Test.ClassName}.{TestContext.CurrentContext.Test.Name}.zip")
            });
        }

        public override BrowserNewContextOptions ContextOptions()
        {
            return new BrowserNewContextOptions()
            {
                BaseURL = $"https://{Environment.GetEnvironmentVariable("containerAppURL", EnvironmentVariableTarget.User)}"
            };
        }

        private async Task TakeScreenshotAsync(int stepNumber, string testName, string stepName)
        {
            var fileName = $"{testName}-{stepNumber}-{stepName}.png";

            await Page.ScreenshotAsync(new()
            {
                Path = fileName
            });

            TestContext.AddTestAttachment(Path.GetFullPath(fileName));
        }
    }
}
