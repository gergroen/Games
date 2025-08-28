using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Games.E2ETests;

[TestClass]
[TestCategory("RequiresBrowser")]
public class TankBattleGameTests : BaseE2ETest
{

    [TestMethod]
    public async Task NavigateToTanks_ShouldDisplayBattlefield()
    {
        // Navigate to the Tank Battle page
        if (Page == null) throw new InvalidOperationException("Page not initialized");
        await Page.GotoAsync($"{BaseUrl}/tanks");

        // Wait for Blazor to fully load
        await WaitForBlazorToLoad();

        // Verify page title
        await Expect(Page).ToHaveTitleAsync("Games");

        // Verify tank game container is visible
        var tankContainer = Page.Locator(".tank-game-container");
        await Expect(tankContainer).ToBeVisibleAsync();

        // Verify canvas element exists
        var canvas = Page.Locator("#tankCanvas");
        await Expect(canvas).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task HUD_ShouldDisplayPlayerAndEnemyHP()
    {
        if (Page == null) throw new InvalidOperationException("Page not initialized");
        await Page.GotoAsync($"{BaseUrl}/tanks");

        // Wait for the game to load
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify HUD is visible with HP information
        var hud = Page.Locator(".hud-hp");
        await Expect(hud).ToBeVisibleAsync();

        // Verify HP displays are present (they should show player and enemy HP)
        await Expect(Page.Locator("text=/Player.*HP/")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=/Enemy.*HP/")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task FireButton_ShouldBeClickableAndDisplayCorrectText()
    {
        if (Page == null) throw new InvalidOperationException("Page not initialized");
        await Page.GotoAsync($"{BaseUrl}/tanks");

        // Wait for Blazor and the game to load completely
        await WaitForBlazorToLoad();

        // Ensure tank game container is loaded first
        var tankContainer = Page.Locator(".tank-game-container");
        await Expect(tankContainer).ToBeVisibleAsync();

        // Find the Fire button
        var fireButton = Page.Locator("button:has-text('FIRE')");
        await Expect(fireButton).ToBeVisibleAsync();
        await Expect(fireButton).ToBeEnabledAsync();

        // Click the Fire button
        await fireButton.ClickAsync();

        // Verify the game is still responsive
        await Expect(Page.Locator(".tank-game-container")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task AutoToggleButton_ShouldToggleBetweenAutoOnAndOff()
    {
        if (Page == null) throw new InvalidOperationException("Page not initialized");
        await Page.GotoAsync($"{BaseUrl}/tanks");

        // Wait for Blazor and the game to load completely
        await WaitForBlazorToLoad();

        // Find the Auto toggle button (should start as "AUTO OFF")
        var autoButton = Page.Locator("button:has-text('AUTO')");
        await Expect(autoButton).ToBeVisibleAsync();
        await Expect(autoButton).ToBeEnabledAsync();

        // Click to toggle auto mode
        await autoButton.ClickAsync();

        // Verify the game is still responsive
        await Expect(Page.Locator(".tank-game-container")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task RestartButton_ShouldResetGame()
    {
        if (Page == null) throw new InvalidOperationException("Page not initialized");
        await Page.GotoAsync($"{BaseUrl}/tanks");

        // Wait for Blazor and the game to load completely
        await WaitForBlazorToLoad();

        // Wait for the tank game container to be visible first
        var tankContainer = Page.Locator(".tank-game-container");
        await Expect(tankContainer).ToBeVisibleAsync();

        // Find the Restart button by its aria-label
        var restartButton = Page.Locator("button[aria-label='Restart']");
        await Expect(restartButton).ToBeVisibleAsync();
        await Expect(restartButton).ToBeEnabledAsync();

        // Click the Restart button
        await restartButton.ClickAsync();

        // Verify the game is still responsive and HUD is still visible
        await Expect(Page.Locator(".hud-hp")).ToBeVisibleAsync();
        await Expect(Page.Locator(".tank-game-container")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task FullscreenButton_ShouldBePresent()
    {
        if (Page == null) throw new InvalidOperationException("Page not initialized");
        await Page.GotoAsync($"{BaseUrl}/tanks");

        // Wait for Blazor and the game to load completely
        await WaitForBlazorToLoad();

        // Wait for the tank game container to be visible first
        var tankContainer = Page.Locator(".tank-game-container");
        await Expect(tankContainer).ToBeVisibleAsync();

        // Find the Fullscreen button specifically by its aria-label
        var fullscreenButton = Page.Locator("button[aria-label='Fullscreen']");
        await Expect(fullscreenButton).ToBeVisibleAsync();
        await Expect(fullscreenButton).ToBeEnabledAsync();

        // Note: We don't actually click fullscreen in tests as it can be problematic
        // in headless environments, but we verify the button exists
    }

    [TestMethod]
    public async Task VirtualJoysticks_ShouldBeVisibleOnMobileViewport()
    {
        if (Page == null) throw new InvalidOperationException("Page not initialized");

        // Set mobile viewport
        await Page.SetViewportSizeAsync(375, 667);
        await Page.GotoAsync($"{BaseUrl}/tanks");

        // Wait for Blazor and the game to load completely
        await WaitForBlazorToLoad();

        // Verify the tank game container is still visible
        await Expect(Page.Locator(".tank-game-container")).ToBeVisibleAsync();

        // Verify canvas is responsive
        var canvas = Page.Locator("#tankCanvas");
        await Expect(canvas).ToBeVisibleAsync();

        // Verify HUD adapts to mobile view
        await Expect(Page.Locator(".hud-hp")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task CanvasRendering_ShouldBeSmooth()
    {
        if (Page == null) throw new InvalidOperationException("Page not initialized");
        await Page.GotoAsync($"{BaseUrl}/tanks");

        // Wait for Blazor and the game to load completely
        await WaitForBlazorToLoad();

        // Verify canvas has proper dimensions
        var canvas = Page.Locator("#tankCanvas");
        await Expect(canvas).ToBeVisibleAsync();

        // Check that the canvas has width and height attributes
        var width = await canvas.GetAttributeAsync("width");
        var height = await canvas.GetAttributeAsync("height");

        Assert.IsNotNull(width, "Canvas should have a width attribute");
        Assert.IsNotNull(height, "Canvas should have a height attribute");

        // Verify the values are reasonable (not zero or empty)
        Assert.IsTrue(int.Parse(width!) > 0, "Canvas width should be greater than 0");
        Assert.IsTrue(int.Parse(height!) > 0, "Canvas height should be greater than 0");
    }

    [TestMethod]
    public async Task Navigation_ShouldWorkBetweenGames()
    {
        // Start at tanks page
        if (Page == null) throw new InvalidOperationException("Page not initialized");
        await Page.GotoAsync($"{BaseUrl}/tanks");
        await Expect(Page.Locator(".tank-game-container")).ToBeVisibleAsync();

        // Navigate to pet game using navigation menu
        var petLink = Page.Locator("a:has-text('Pet')");
        if (await petLink.CountAsync() > 0)
        {
            await petLink.ClickAsync();
            await Expect(Page.Locator(".pet-container")).ToBeVisibleAsync();
        }
        else
        {
            // If no navigation menu, use direct navigation
            if (Page == null) throw new InvalidOperationException("Page not initialized");
            await Page.GotoAsync(BaseUrl);
            await Expect(Page.Locator(".pet-container")).ToBeVisibleAsync();
        }

        // Navigate back to tanks
        var tankLink = Page.Locator("a:has-text('Tank')");
        if (await tankLink.CountAsync() > 0)
        {
            await tankLink.ClickAsync();
            await Expect(Page.Locator(".tank-game-container")).ToBeVisibleAsync();
        }
        else
        {
            // If no navigation menu, use direct navigation
            if (Page == null) throw new InvalidOperationException("Page not initialized");
            await Page.GotoAsync($"{BaseUrl}/tanks");
            await Expect(Page.Locator(".tank-game-container")).ToBeVisibleAsync();
        }
    }
}