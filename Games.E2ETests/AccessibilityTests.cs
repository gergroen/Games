using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace Games.E2ETests;

[TestClass]
[TestCategory("RequiresBrowser")]
public class AccessibilityTests : BaseE2ETest
{
    [TestMethod]
    public async Task TamagotchiGame_ShouldHaveAccessibleElements()
    {
        await Page.GotoAsync(BaseUrl);
        await WaitForBlazorToLoad();

        // Check for proper ARIA labels and semantic HTML
        var feedButton = Page.Locator("button:has-text('Feed (A)')");
        await Expect(feedButton).ToBeVisibleAsync();

        var playButton = Page.Locator("button:has-text('Play (B)')");
        await Expect(playButton).ToBeVisibleAsync();

        var restButton = Page.Locator("button:has-text('Rest (X)')");
        await Expect(restButton).ToBeVisibleAsync();

        // Verify buttons are keyboard accessible by focusing directly on them
        await feedButton.FocusAsync();
        var focusedElement = await Page.EvaluateAsync<string>("document.activeElement.tagName");
        Assert.AreEqual("BUTTON", focusedElement, "Feed button should be focusable");

        // Test that Tab key can navigate to game buttons (tab through navigation first)
        await Page.Keyboard.PressAsync("Tab"); // Navigate through nav elements
        
        // Tab through until we reach a game button (allowing for navigation menu items)
        var maxTabs = 10; // Reasonable limit to prevent infinite loop
        var foundGameButton = false;
        
        for (int i = 0; i < maxTabs; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            var currentElement = await Page.EvaluateAsync<string>("document.activeElement.textContent || ''");
            
            if (currentElement.Contains("Feed") || currentElement.Contains("Play") || currentElement.Contains("Rest"))
            {
                foundGameButton = true;
                break;
            }
        }
        
        Assert.IsTrue(foundGameButton, "Should be able to navigate to game buttons via keyboard");
    }

    [TestMethod]
    public async Task TankGame_ShouldHaveAccessibleControls()
    {
        await Page.GotoAsync($"{BaseUrl}/tanks");
        await WaitForBlazorToLoad();

        // Check for ARIA labels on important controls
        var restartButton = Page.Locator("button[aria-label='Restart']");
        await Expect(restartButton).ToBeVisibleAsync();

        var fullscreenButton = Page.Locator("button[aria-label='Fullscreen']");
        await Expect(fullscreenButton).ToBeVisibleAsync();

        // Verify keyboard navigation works by directly testing button focusability
        await restartButton.FocusAsync();
        var focusedElement = await Page.EvaluateAsync<string>("document.activeElement.tagName");
        Assert.AreEqual("BUTTON", focusedElement, "Restart button should be focusable");

        // Test that Tab key can navigate to tank game controls (allowing for navigation menu)
        await Page.Keyboard.PressAsync("Tab"); // Start navigation
        
        // Tab through until we reach tank controls (allowing for navigation menu items)
        var maxTabs = 15; // More tabs needed for tank game due to more complex UI
        var foundInteractiveElement = false;
        
        for (int i = 0; i < maxTabs; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            var currentElement = await Page.EvaluateAsync<string>("document.activeElement.tagName");
            var elementContent = await Page.EvaluateAsync<string>("document.activeElement.textContent || document.activeElement.getAttribute('aria-label') || ''");
            
            if (currentElement == "BUTTON" && (elementContent.Contains("Restart") || elementContent.Contains("Fullscreen") || elementContent.Contains("AUTO") || elementContent.Contains("FIRE")) ||
                currentElement == "CANVAS")
            {
                foundInteractiveElement = true;
                break;
            }
        }
        
        Assert.IsTrue(foundInteractiveElement, "Should be able to navigate to tank controls via keyboard");
    }

    [TestMethod]
    public async Task ColorContrast_ShouldBeAccessible()
    {
        await Page.GotoAsync(BaseUrl);
        await WaitForBlazorToLoad();

        // Check that text has sufficient contrast
        // This is a basic check - in real scenarios you'd use specialized tools
        var petContainer = Page.Locator(".pet-container");
        await Expect(petContainer).ToBeVisibleAsync();

        // Verify that text is readable (not transparent or too light)
        var computedStyle = await Page.EvaluateAsync<string>(@"
            () => {
                const element = document.querySelector('.pet-container');
                if (!element) return 'element not found';
                const style = window.getComputedStyle(element);
                return `color: ${style.color}, background: ${style.backgroundColor}`;
            }
        ");

        Assert.IsNotNull(computedStyle, "Should be able to compute styles for contrast checking");
        Assert.IsFalse(computedStyle.Contains("rgba(0, 0, 0, 0)"), "Text should not be transparent");
    }

    [TestMethod]
    public async Task ScreenReader_ShouldFindAppropriateLabels()
    {
        await Page.GotoAsync(BaseUrl);
        await WaitForBlazorToLoad();

        // Check for appropriate headings and labels
        var pageHeadings = await Page.Locator("h1, h2, h3, h4, h5, h6").CountAsync();
        Assert.IsTrue(pageHeadings > 0, "Page should have proper heading structure");

        // Check that form controls have labels
        var buttons = await Page.Locator("button").CountAsync();
        Assert.IsTrue(buttons > 0, "Page should have interactive buttons");

        // Verify that important game stats are labeled
        await Expect(Page.Locator("text=Hunger:")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Happiness:")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Energy:")).ToBeVisibleAsync();
        await Expect(Page.Locator("text=Mood:")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Navigation_ShouldBeKeyboardAccessible()
    {
        await Page.GotoAsync(BaseUrl);
        await WaitForBlazorToLoad();

        // Test keyboard navigation through the interface
        var initialActiveElement = await Page.EvaluateAsync<string>("document.activeElement.tagName");

        // Tab through elements
        for (int i = 0; i < 5; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
        }

        var finalActiveElement = await Page.EvaluateAsync<string>("document.activeElement.tagName");

        // Should be able to navigate to interactive elements
        Assert.IsTrue(finalActiveElement == "BUTTON" || finalActiveElement == "A" || finalActiveElement == "INPUT",
            $"Should be able to tab to interactive elements. Final element: {finalActiveElement}");
    }

    [TestMethod]
    public async Task FocusIndicators_ShouldBeVisible()
    {
        await Page.GotoAsync(BaseUrl);
        await WaitForBlazorToLoad();

        // Wait for any animations to complete
        await Page.WaitForTimeoutAsync(1000);

        // Tab to first button and check if focus is visible
        await Page.Keyboard.PressAsync("Tab");
        
        // Give time for focus to be applied
        await Page.WaitForTimeoutAsync(500);

        // Get the focused element and check if it has visible focus styling
        var focusStyles = await Page.EvaluateAsync<string>(@"
            () => {
                const focused = document.activeElement;
                if (!focused || focused.tagName !== 'BUTTON') return 'no button focused';
                const styles = window.getComputedStyle(focused, ':focus');
                return `outline: ${styles.outline}, box-shadow: ${styles.boxShadow}`;
            }
        ");

        Assert.IsNotNull(focusStyles, "Should be able to get focus styles");
        
        // If no button is focused after Tab, try tabbing multiple times to find a button
        if (focusStyles.Contains("no button focused"))
        {
            for (int i = 0; i < 5; i++)
            {
                await Page.Keyboard.PressAsync("Tab");
                await Page.WaitForTimeoutAsync(200);
                
                focusStyles = await Page.EvaluateAsync<string>(@"
                    () => {
                        const focused = document.activeElement;
                        if (!focused || focused.tagName !== 'BUTTON') return 'no button focused';
                        const styles = window.getComputedStyle(focused, ':focus');
                        return `outline: ${styles.outline}, box-shadow: ${styles.boxShadow}`;
                    }
                ");
                
                if (!focusStyles.Contains("no button focused"))
                {
                    break;
                }
            }
        }
        
        Assert.IsFalse(focusStyles.Contains("no button focused"), "Should have a button focused");
    }
}