using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Games.E2ETests;

[TestClass]
public abstract class BaseE2ETest 
{
    protected const string BaseUrl = "http://localhost:5080";
    protected static IPlaywright? PlaywrightInstance;
    protected static IBrowser? BrowserInstance;
    protected IBrowserContext? Context;
    protected IPage? Page;

    [TestInitialize]
    public async Task SetupTest()
    {
        // Initialize Playwright if not already done
        if (PlaywrightInstance == null)
        {
            PlaywrightInstance = await Playwright.CreateAsync();
        }

        // Get optimized launch options for system browser
        var launchOptions = GetOptimizedLaunchOptions();

        // Create browser instance if not already created
        if (BrowserInstance == null)
        {
            BrowserInstance = await PlaywrightInstance.Chromium.LaunchAsync(launchOptions);
        }

        // Create new context for this test
        Context = await BrowserInstance.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        // Create new page for this test
        Page = await Context.NewPageAsync();

        // Set reasonable timeouts for CI environments
        Page.SetDefaultTimeout(30000); // 30 seconds
        Page.SetDefaultNavigationTimeout(30000); // 30 seconds
    }

    [TestCleanup]
    public async Task CleanupTest()
    {
        if (Context != null)
        {
            await Context.CloseAsync();
        }
    }

    /// <summary>
    /// Wait for Blazor to fully render by waiting for the app container to be visible
    /// and the loading indicator to disappear
    /// </summary>
    protected async Task WaitForBlazorToLoad()
    {
        if (Page == null) throw new InvalidOperationException("Page not initialized");

        // Wait for Blazor to load (loading indicator should disappear)
        await Page.WaitForSelectorAsync("#app .loading-progress", new() { State = WaitForSelectorState.Hidden });

        // Wait for network to be idle after initial load
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Get browser launch options optimized for CI environments with system browser
    /// </summary>
    private static BrowserTypeLaunchOptions GetOptimizedLaunchOptions()
    {
        var options = new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[]
            {
                "--no-sandbox",
                "--disable-setuid-sandbox", 
                "--disable-dev-shm-usage",
                "--disable-gpu",
                "--disable-web-security",
                "--disable-background-timer-throttling",
                "--disable-backgrounding-occluded-windows",
                "--disable-renderer-backgrounding",
                "--disable-extensions",
                "--disable-plugins",
                "--disable-default-apps"
            }
        };

        // Use system Chrome if available
        if (File.Exists("/usr/bin/google-chrome"))
        {
            options.ExecutablePath = "/usr/bin/google-chrome";
        }
        else if (File.Exists("/usr/bin/chromium-browser"))
        {
            options.ExecutablePath = "/usr/bin/chromium-browser";
        }
        else
        {
            // Fallback: let Playwright use its own browsers if available
            options.ExecutablePath = null;
        }

        return options;
    }
}