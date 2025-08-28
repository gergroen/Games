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

All browser tests are marked with `[TestCategory("RequiresBrowser")]` to allow selective execution.

## Coding Copilot Integration

The E2E test runner (`./run-e2e-tests.sh`) is specifically designed for automated environments like GitHub Copilot. It provides:

### Automated Workflow
1. **Environment Setup**: Automatically checks and installs .NET 9.0 SDK
2. **Application Management**: Starts the Games application if not running
3. **Graceful Degradation**: Falls back to smoke tests if browser installation fails
4. **Clear Reporting**: Provides detailed status and results

### Usage Examples
```bash
# Standard automated testing (recommended)
./run-e2e-tests.sh

# Fast validation (smoke tests only)
./run-e2e-tests.sh --smoke-only

# Debugging (verbose output)
./run-e2e-tests.sh --verbose
```

### Expected Behavior
- **Success Case**: Both smoke and browser tests pass
- **Degraded Case**: Smoke tests pass, browser tests skipped (still returns success)
- **Failure Case**: Smoke tests fail (indicates application issues)

The script returns exit code 0 for success (including degraded scenarios) and exit code 1 for critical failures.

## Running Tests

### Quick Start (Recommended for Coding Copilot)

**Use the automated E2E test runner (handles everything automatically):**
```bash
# From the repository root directory
./run-e2e-tests.sh
```

This script automatically:
- Installs .NET 9.0 SDK if needed
- Starts the Games application if not running
- Builds and runs tests with graceful degradation
- Provides clear feedback on results

**Available options:**
```bash
./run-e2e-tests.sh --smoke-only    # Run only smoke tests (fastest, most reliable)
./run-e2e-tests.sh --verbose       # Enable detailed output
./run-e2e-tests.sh --help          # Show help
```

### Manual Testing (Advanced Users)

**Prerequisites:**
- .NET 9.0 SDK
- Running Games application (on http://localhost:5080)

**Run smoke tests only (no browser required):**
```bash
dotnet test --filter "TestCategory!=RequiresBrowser" --logger "console;verbosity=detailed"
```

**Run all tests (requires Playwright browsers):**
```bash
# Install Playwright browsers first
pwsh bin/Debug/net9.0/playwright.ps1 install --with-deps

# Run all tests
dotnet test --logger "console;verbosity=detailed"
```

### CI/CD Integration

The GitHub Actions workflow automatically:

1. **Installs dependencies** and builds the application
2. **Attempts Playwright browser installation** with retry logic
3. **Runs smoke tests first** (always works, no browser required)
4. **Conditionally runs full E2E tests** if browsers are available
5. **Continues deployment** even if some tests fail
6. **Uploads test results** as artifacts for review

This ensures deployment isn't blocked by browser installation issues while still providing comprehensive testing when possible.

## Test Coverage

### Tamagotchi Game Tests
- Navigation and UI element validation
- Action button functionality (Feed, Play, Rest with gamepad indicators)
- Gamepad connection status display
- Mobile responsive design testing
- Keyboard navigation and accessibility

### Tank Battle Game Tests
- Battlefield rendering and canvas display validation
- HUD functionality with HP display testing
- Fire button and Auto toggle controls
- Restart and Fullscreen button functionality
- Mobile viewport virtual joysticks testing
- Canvas rendering performance validation
- Cross-game navigation testing

### Smoke Tests
- Basic application availability
- Page routing functionality
- Static asset serving
- Core infrastructure validation

## Troubleshooting

### Playwright Browser Installation Issues
If browser installation fails:
1. Check network connectivity
2. Try alternative installation: `dotnet tool install --global Microsoft.Playwright.CLI`
3. Use smoke tests for basic validation: `dotnet test --filter "TestCategory!=RequiresBrowser"`

### Common Issues
- **Application not running**: Ensure `dotnet run` is active on port 5080
- **Browser tests failing**: Verify Playwright browsers are installed
- **Timeout errors**: Increase test timeout values for slow environments

## Architecture

The test design prioritizes reliability and flexibility:
- **Graceful degradation**: Tests run at multiple levels (smoke → browser)
- **Selective execution**: Tests can be filtered by category
- **Retry mechanisms**: Browser installation has multiple fallback strategies
- **Result preservation**: Test results are uploaded regardless of outcome

This approach ensures consistent CI/CD pipeline execution while maintaining comprehensive test coverage when possible.

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