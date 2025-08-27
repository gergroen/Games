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
        // Wait for Blazor to load (loading indicator should disappear)
        await Page.WaitForSelectorAsync("#app .loading-progress", new() { State = WaitForSelectorState.Hidden });

        // Wait for network to be idle after initial load
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}