using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace Games.E2ETests;

public abstract class BaseE2ETest : PageTest
{
    protected const string BaseUrl = "http://localhost:5080";

    [TestInitialize]
    public async Task SetupTest()
    {
        // Set reasonable timeouts for CI environments
        Page.SetDefaultTimeout(30000); // 30 seconds
        Page.SetDefaultNavigationTimeout(30000); // 30 seconds

        // Set viewport for consistent testing
        await Page.SetViewportSizeAsync(1280, 720);
    }

    /// <summary>
    /// Wait for Blazor to fully render by waiting for the app container to be visible
    /// and the loading indicator to disappear
    /// </summary>
    protected async Task WaitForBlazorToLoad()
    {
        try
        {
            // Wait for Blazor to load (loading indicator should disappear)
            await Page.WaitForSelectorAsync("#app .loading-progress", new() { State = WaitForSelectorState.Hidden, Timeout = 45000 });
        }
        catch (TimeoutException)
        {
            // If loading indicator doesn't disappear, check if Blazor loaded anyway
            // Sometimes in CI environments the loading indicator might not behave as expected
            var hasContent = await Page.Locator("#app > div:not(.loading-progress):not(.loading-progress-text)").CountAsync() > 0;
            if (!hasContent)
            {
                throw new TimeoutException("Blazor application failed to load within 45 seconds");
            }
        }

        // Wait for network to be idle after initial load with a reasonable timeout
        try
        {
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 30000 });
        }
        catch (TimeoutException)
        {
            // In CI environments, network might not go idle quickly
            // Fall back to waiting for DOM content to be stable
            await Page.WaitForTimeoutAsync(2000);
        }
    }
}