# E2E Testing for Games Application

This project contains comprehensive end-to-end tests for both the Tamagotchi and Tank Battle games, implemented using Microsoft Playwright and MSTest.

## Test Structure

### Smoke Tests (No Browser Required)
- **SmokeTests.cs**: Basic HTTP tests that validate the application is running and accessible
  - Application response validation
  - Index page content validation
  - Tank page accessibility 
  - Static asset accessibility (CSS, JS)

### Browser-Dependent E2E Tests
- **TamagotchiGameTests.cs**: 7 comprehensive tests for the virtual pet game
- **TankBattleGameTests.cs**: 9 comprehensive tests for the tank battle game
- **AccessibilityTests.cs**: 6 tests for keyboard navigation, ARIA labels, and screen reader support
- **PWATests.cs**: 5 tests for Progressive Web App features (manifest, service worker, offline support)
- **PerformanceTests.cs**: 6 tests for load times, responsiveness, and resource optimization

All browser tests are marked with `[TestCategory("RequiresBrowser")]` to allow selective execution.

## Prerequisites

1. **.NET 9.0 SDK** - Required for running the tests
2. **Playwright Browsers** - Must be installed before running tests
3. **Running Application** - The Games application must be running on `http://localhost:5080`

### Install .NET 9.0 SDK
```bash
# Linux/macOS
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0
export PATH="$HOME/.dotnet:$PATH"

# Windows
# Download from https://dotnet.microsoft.com/download/dotnet/9.0
```

### Start the Games Application
```bash
# From the Games directory
dotnet run --project Games
```
The application should be running on `http://localhost:5080`

## Running Tests

### Setup (First Time)
```bash
# Build the test project
cd Games.E2ETests
dotnet build

# Install Playwright browsers
pwsh bin/Debug/net9.0/playwright.ps1 install --with-deps
```

### Run Tests

**Run smoke tests only (no browser required):**
```bash
dotnet test --filter "TestCategory!=RequiresBrowser" --logger "console;verbosity=detailed"
```

**Run all tests (requires Playwright browsers):**
```bash
dotnet test --logger "console;verbosity=detailed"
```

**Run specific test classes:**
```bash
dotnet test --filter "ClassName=TamagotchiGameTests"
dotnet test --filter "ClassName=TankBattleGameTests"
```

## Test Coverage

### Tamagotchi Game Tests (7 Tests)
- **NavigateToTamagotchi_ShouldDisplayPetWithStats** - Verifies pet displays with hunger, happiness, energy, and mood stats
- **FeedButton_ShouldBeClickableAndDisplayCorrectText** - Tests Feed (A) button functionality
- **PlayButton_ShouldBeClickableAndDisplayCorrectText** - Tests Play (B) button functionality  
- **RestButton_ShouldBeClickableAndDisplayCorrectText** - Tests Rest (X) button functionality
- **GamepadConnectionStatus_ShouldBeDisplayed** - Verifies gamepad connection status display
- **ResponsiveDesign_ShouldAdaptToMobileViewport** - Tests mobile responsive design
- **KeyboardNavigation_ShouldWorkWithTabAndEnter** - Validates keyboard accessibility

### Tank Battle Game Tests (9 Tests)
- **NavigateToTanks_ShouldDisplayBattlefield** - Verifies battlefield and canvas rendering
- **HUD_ShouldDisplayPlayerAndEnemyHP** - Tests HUD HP display functionality
- **FireButton_ShouldBeClickableAndDisplayCorrectText** - Tests Fire button functionality
- **AutoToggleButton_ShouldToggleBetweenAutoOnAndOff** - Tests Auto toggle functionality
- **RestartButton_ShouldResetGame** - Tests Restart button functionality
- **FullscreenButton_ShouldBePresent** - Verifies Fullscreen button presence
- **VirtualJoysticks_ShouldBeVisibleOnMobileViewport** - Tests mobile virtual joysticks
- **CanvasRendering_ShouldBeSmooth** - Validates canvas rendering performance
- **Navigation_ShouldWorkBetweenGames** - Tests cross-game navigation

### Smoke Tests (4 Tests)
- **Application_ShouldRespond** - Basic application availability
- **Application_ShouldServeIndexPage** - Index page routing functionality
- **TanksPage_ShouldBeAccessible** - Tank page routing functionality
- **StaticAssets_ShouldBeAccessible** - Static asset serving validation

### Accessibility Tests (6 Tests)
- **TamagotchiGame_ShouldHaveAccessibleElements** - ARIA labels and keyboard navigation for pet game
- **TankGame_ShouldHaveAccessibleControls** - Accessibility of tank game controls
- **ColorContrast_ShouldBeAccessible** - Basic color contrast validation
- **ScreenReader_ShouldFindAppropriateLabels** - Proper heading structure and labels
- **Navigation_ShouldBeKeyboardAccessible** - Keyboard navigation through interface
- **FocusIndicators_ShouldBeVisible** - Visual focus indicators for keyboard users

### PWA Tests (5 Tests)
- **Manifest_ShouldBeAccessible** - Web App Manifest validation and accessibility
- **ServiceWorker_ShouldBeRegistered** - Service Worker registration verification
- **AppIcons_ShouldBeAccessible** - App icon and favicon accessibility
- **OfflineSupport_ShouldShowAppropriateMessage** - Offline functionality testing
- **InstallPrompt_ShouldBeAvailable** - PWA installability criteria validation

### Performance Tests (6 Tests)
- **ApplicationLoad_ShouldBeFast** - Application load time measurement
- **GameInteractions_ShouldBeResponsive** - Game interaction response time testing
- **TankCanvas_ShouldRenderSmoothly** - Canvas rendering performance validation
- **MemoryUsage_ShouldNotLeak** - Memory leak detection during gameplay
- **ResourceSizes_ShouldBeOptimized** - Resource size optimization verification
- **NetworkRequests_ShouldBeMinimal** - Network request count and size monitoring

## CI/CD Integration

The GitHub Actions workflow automatically:

1. **Installs dependencies** and builds the application
2. **Attempts Playwright browser installation** with retry logic
3. **Runs smoke tests first** (always works, no browser required)
4. **Conditionally runs full E2E tests** if browsers are available
5. **Continues deployment** even if some tests fail
6. **Uploads test results** as artifacts for review

This ensures deployment isn't blocked by browser installation issues while still providing comprehensive testing when possible.

## Test Architecture

The tests inherit from `PageTest` provided by `Microsoft.Playwright.MSTest`, which provides:
- Automatic browser lifecycle management
- Page isolation between tests
- Built-in test fixtures and setup/teardown
- Cross-browser testing capabilities

Each test follows the pattern:
1. Navigate to the target page
2. Verify expected elements are visible and functional
3. Perform user interactions (clicks, input, etc.)
4. Assert the application responds correctly

The test design prioritizes reliability and flexibility:
- **Graceful degradation**: Tests run at multiple levels (smoke → browser)
- **Selective execution**: Tests can be filtered by category
- **Retry mechanisms**: Browser installation has multiple fallback strategies
- **Result preservation**: Test results are uploaded regardless of outcome

## Troubleshooting

### Browser Installation Issues
If browser installation fails:
```bash
# Try installing specific browser
pwsh bin/Debug/net9.0/playwright.ps1 install chromium --with-deps

# Alternative installation method
dotnet tool install --global Microsoft.Playwright.CLI
```

### Common Issues
- **Application not running**: Ensure `dotnet run --project Games` is active on port 5080
- **Browser tests failing**: Verify Playwright browsers are installed
- **Port conflicts**: Ensure port 5080 is available (`curl http://localhost:5080`)
- **Timeout errors**: Increase test timeout values for slow environments
- **Network timeouts**: Increase timeout values in test methods if needed

### Verification Commands
```bash
# Check if application is running
curl http://localhost:5080

# Verify browser installation
pwsh bin/Debug/net9.0/playwright.ps1 install --dry-run
```

## Contributing

When adding new tests:
1. Follow the existing naming convention: `FeatureName_ShouldExpectedBehavior`
2. Include appropriate assertions for UI elements and functionality
3. Add mobile viewport testing where relevant
4. Mark browser tests with `[TestCategory("RequiresBrowser")]`
5. Update this README with new test descriptions