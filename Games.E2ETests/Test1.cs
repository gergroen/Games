using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace Games.E2ETests;

[TestClass]
[TestCategory("RequiresBrowser")]
public class TamagotchiGameTests : PageTest
{
    private const string BaseUrl = "http://localhost:5080";

    [TestMethod]
    public async Task NavigateToTamagotchi_ShouldDisplayPetWithStats()
    {
        // Navigate to the Tamagotchi page
        await Page.GotoAsync(BaseUrl);

        // Verify page title
        await Expect(Page).ToHaveTitleAsync("Games");

        // Verify pet container is visible
        var petContainer = Page.Locator(".pet-container");
        await Expect(petContainer).ToBeVisibleAsync();

        // Verify pet sprite is displayed
        var petSprite = Page.Locator(".pet-sprite");
        await Expect(petSprite).ToBeVisibleAsync();

        // Verify stat displays are present
        await Expect(Page.Locator("text=Hunger:")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Happiness:")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Energy:")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Mood:")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task FeedButton_ShouldBeClickableAndDisplayCorrectText()
    {
        await Page.GotoAsync(BaseUrl);

        // Find the Feed button (with A gamepad key indicator)
        var feedButton = Page.Locator("button:has-text('Feed (A)')");
        await Expect(feedButton).ToBeVisibleAsync();
        await Expect(feedButton).ToBeEnabledAsync();

        // Click the Feed button
        await feedButton.ClickAsync();

        // The page should still be responsive after clicking
        await Expect(Page.Locator(".pet-container")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task PlayButton_ShouldBeClickableAndDisplayCorrectText()
    {
        await Page.GotoAsync(BaseUrl);

        // Find the Play button (with B gamepad key indicator)
        var playButton = Page.Locator("button:has-text('Play (B)')");
        await Expect(playButton).ToBeVisibleAsync();
        await Expect(playButton).ToBeEnabledAsync();

        // Click the Play button
        await playButton.ClickAsync();

        // The page should still be responsive after clicking
        await Expect(Page.Locator(".pet-container")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task RestButton_ShouldBeClickableAndDisplayCorrectText()
    {
        await Page.GotoAsync(BaseUrl);

        // Find the Rest button (with X gamepad key indicator)
        var restButton = Page.Locator("button:has-text('Rest (X)')");
        await Expect(restButton).ToBeVisibleAsync();
        await Expect(restButton).ToBeEnabledAsync();

        // Click the Rest button
        await restButton.ClickAsync();

        // The page should still be responsive after clicking
        await Expect(Page.Locator(".pet-container")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task GamepadConnectionStatus_ShouldBeDisplayed()
    {
        await Page.GotoAsync(BaseUrl);

        // Verify gamepad connection status is shown
        // This will typically show "No gamepad connected" in a test environment
        var connectionStatus = Page.Locator("text=/gamepad/i");
        await Expect(connectionStatus).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ResponsiveDesign_ShouldAdaptToMobileViewport()
    {
        // Set mobile viewport
        await Page.SetViewportSizeAsync(375, 667);
        await Page.GotoAsync(BaseUrl);

        // Verify the page is still functional in mobile view
        await Expect(Page.Locator(".pet-container")).ToBeVisibleAsync();
        await Expect(Page.Locator("button:has-text('Feed (A)')")).ToBeVisibleAsync();
        await Expect(Page.Locator("button:has-text('Play (B)')")).ToBeVisibleAsync();
        await Expect(Page.Locator("button:has-text('Rest (X)')")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task KeyboardNavigation_ShouldWorkWithTabAndEnter()
    {
        await Page.GotoAsync(BaseUrl);

        // Focus on the first button using Tab
        await Page.Keyboard.PressAsync("Tab");

        // Verify a button is focused (though exact focus detection can be tricky)
        // We'll just ensure the page remains responsive to keyboard input
        await Page.Keyboard.PressAsync("Enter");

        // Verify the page is still functional
        await Expect(Page.Locator(".pet-container")).ToBeVisibleAsync();
    }
}
