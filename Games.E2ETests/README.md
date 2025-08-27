# Games E2E Tests

This project contains Playwright-based end-to-end tests for the Games Blazor WebAssembly application, written in C# using MSTest framework.

## Overview

The E2E test suite validates both games included in the application:

### Virtual Pet (Tamagotchi) Game Tests
- **NavigateToTamagotchi_ShouldDisplayPetWithStats** - Verifies pet displays with hunger, happiness, energy, and mood stats
- **FeedButton_ShouldBeClickableAndDisplayCorrectText** - Tests Feed (A) button functionality
- **PlayButton_ShouldBeClickableAndDisplayCorrectText** - Tests Play (B) button functionality  
- **RestButton_ShouldBeClickableAndDisplayCorrectText** - Tests Rest (X) button functionality
- **GamepadConnectionStatus_ShouldBeDisplayed** - Verifies gamepad connection status display
- **ResponsiveDesign_ShouldAdaptToMobileViewport** - Tests mobile responsive design
- **KeyboardNavigation_ShouldWorkWithTabAndEnter** - Validates keyboard accessibility

### Tank Battle Game Tests  
- **NavigateToTanks_ShouldDisplayBattlefield** - Verifies battlefield and canvas rendering
- **HUD_ShouldDisplayPlayerAndEnemyHP** - Tests HUD HP display functionality
- **FireButton_ShouldBeClickableAndDisplayCorrectText** - Tests Fire button functionality
- **AutoToggleButton_ShouldToggleBetweenAutoOnAndOff** - Tests Auto toggle functionality
- **RestartButton_ShouldResetGame** - Tests Restart button functionality
- **FullscreenButton_ShouldBePresent** - Verifies Fullscreen button presence
- **VirtualJoysticks_ShouldBeVisibleOnMobileViewport** - Tests mobile virtual joysticks
- **CanvasRendering_ShouldBeSmooth** - Validates canvas rendering performance
- **Navigation_ShouldWorkBetweenGames** - Tests cross-game navigation

## Prerequisites

1. **.NET 9.0 SDK** - Required for running the tests
2. **Playwright Browsers** - Must be installed before running tests
3. **Running Application** - The Games application must be running on `http://localhost:5080`

## Setup Instructions

### 1. Install .NET 9.0 SDK
```bash
# Linux/macOS
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0
export PATH="$HOME/.dotnet:$PATH"

# Windows
# Download from https://dotnet.microsoft.com/download/dotnet/9.0
```

### 2. Build the Test Project
```bash
cd Games.E2ETests
dotnet build
```

### 3. Install Playwright Browsers
```bash
# From the Games.E2ETests directory
pwsh bin/Debug/net9.0/playwright.ps1 install

# Alternative using PowerShell on Windows
.\bin\Debug\net9.0\playwright.ps1 install
```

### 4. Start the Games Application
```bash
# From the Games directory
dotnet run --project Games
```
The application should be running on `http://localhost:5080`

## Running Tests

### Run All Tests
```bash
dotnet test Games.E2ETests
```

### Run Specific Test Class
```bash
dotnet test Games.E2ETests --filter "ClassName=TamagotchiGameTests"
dotnet test Games.E2ETests --filter "ClassName=TankBattleGameTests"
```

### Run with Detailed Output
```bash
dotnet test Games.E2ETests --logger "console;verbosity=detailed"
```

### Run with Custom Settings
```bash
dotnet test Games.E2ETests --settings playwright.runsettings
```

## Test Configuration

The tests are configured to:
- Run against `http://localhost:5080` (update `BaseUrl` constant if needed)
- Use Chromium browser in headless mode
- Support both desktop and mobile viewport testing
- Validate responsive design and accessibility features

## Troubleshooting

### Browser Installation Issues
If browser installation fails:
```bash
# Try installing specific browser
pwsh bin/Debug/net9.0/playwright.ps1 install chromium

# Or with dependencies
pwsh bin/Debug/net9.0/playwright.ps1 install chromium --with-deps
```

### Application Not Running
Ensure the Games application is running before executing tests:
```bash
curl http://localhost:5080
```

### Test Failures
Common issues:
- **Application not started** - Start with `dotnet run --project Games`
- **Browsers not installed** - Run the Playwright install command
- **Port conflicts** - Ensure port 5080 is available
- **Network timeouts** - Increase timeout values in test methods if needed

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

## Contributing

When adding new tests:
1. Follow the existing naming convention: `FeatureName_ShouldExpectedBehavior`
2. Include appropriate assertions for UI elements and functionality
3. Add mobile viewport testing where relevant
4. Update this README with new test descriptions