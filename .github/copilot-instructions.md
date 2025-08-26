# Games Repository Instructions

Games is a Blazor WebAssembly application featuring two interactive browser games: a Tamagotchi-style virtual pet game and a real-time tank battle game. Both games support gamepad input alongside mouse/touch controls.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Quick Start

For urgent fixes or small changes:
1. Install .NET 9.0 SDK (see Prerequisites section)
2. `dotnet build Games.sln` (2-4 seconds)
3. `dotnet run --project Games` (8-12 seconds startup)
4. Navigate to http://localhost:5080
5. Test both games: `/` (Tamagotchi) and `/tanks` (Tank Battle)
6. `dotnet format --verify-no-changes` before committing

## Working Effectively

### Prerequisites and Setup
- **CRITICAL**: Install .NET 9.0 SDK first:
  ```bash
  wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
  chmod +x dotnet-install.sh
  ./dotnet-install.sh --channel 9.0
  export PATH="$HOME/.dotnet:$PATH"
  ```
- Verify installation: `dotnet --version` should show 9.0.x
- **IMPORTANT**: Always export PATH before running dotnet commands in new shells
- Supported platforms: Windows, macOS, Linux (Ubuntu 18.04+)

### Build and Run Commands
- **Clean build**: `dotnet clean Games.sln && dotnet build Games.sln` -- takes 4-6 seconds. NEVER CANCEL. Set timeout to 30+ seconds.
- **Quick build**: `dotnet build Games.sln` -- takes 2-4 seconds when no changes to dependencies.
- **Run application**: `dotnet run --project Games` -- startup takes 8-12 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
  - Application runs on: http://localhost:5080 (HTTP) and https://localhost:7042 (HTTPS)
  - Look for: "Now listening on: http://[::]:5080" to confirm successful startup
- **Watch mode** (for development): `dotnet watch --project Games` -- auto-rebuilds on file changes

### Code Quality
- **Format code**: `dotnet format` -- takes 5-10 seconds. NEVER CANCEL. Set timeout to 30+ seconds.
- **Verify formatting**: `dotnet format --verify-no-changes` -- takes 2-3 seconds. Must pass before committing.
- **REQUIRED**: All code must pass formatting before any commit
- No unit tests exist in this project (games are interaction-heavy and tested manually).
- Code style: C# conventions with implicit usings enabled, nullable reference types enabled

## Validation Scenarios

**ALWAYS manually validate any code changes by running both games through these scenarios:**

### Tamagotchi Game Validation (Default route: `/`)
1. Navigate to root page - should display pet with stats (Hunger, Happiness, Energy, Mood)
2. Click "Feed (A)" button - hunger should decrease, happiness may increase, pet face should change
3. Click "Play (B)" button - happiness should increase, energy may decrease
4. Click "Rest (X)" button - energy should increase
5. Verify pet's appearance and mood change based on stats
6. Check gamepad connection status is displayed
7. **Test responsiveness**: Resize browser window, check mobile viewport
8. **Accessibility**: Verify keyboard navigation works with Tab/Enter/Space

### Tanks Game Validation (Route: `/tanks`)
1. Navigate to `/tanks` - should display battlefield with player tank (red) and enemy tank (green)
2. Verify HUD shows "Player HP: 100 | Enemy HP: 100"
3. Click "FIRE" button - should trigger combat, HP values may change
4. Click "Restart" button - should reset both HP to 100 and respawn tanks
5. Test "AUTO OFF"/"AUTO ON" toggle button functionality
6. Verify virtual joysticks are visible on screen
7. Check for game sounds when firing (audio is generated via Web Audio API)
8. **Fullscreen testing**: Click fullscreen button, verify layout adapts
9. **Touch controls**: Test virtual joysticks on mobile/tablet viewports
10. **Performance**: Verify smooth 60fps animation during combat

### Performance Validation
- Both games should maintain smooth animation (60fps target)
- Canvas should resize properly when browser window changes
- Mobile touch controls should work on virtual joysticks
- **Memory usage**: Games should not cause memory leaks during extended play
- **PWA functionality**: App should work offline after initial load

### Cross-Browser Testing
- **Primary browsers**: Chrome, Firefox, Safari, Edge (latest 2 versions)
- **Gamepad support**: Test with Xbox/PlayStation controllers if available
- **Mobile browsers**: iOS Safari, Chrome Mobile, Samsung Internet

## Repository Structure

### Architecture Overview
The Games repository follows a clean Blazor WebAssembly architecture with clear separation of concerns:
- **Services**: Game logic and state management
- **Pages**: UI components and user interaction
- **Models**: Data structures and game entities
- **JavaScript Interop**: Browser APIs (gamepad, canvas, audio)
- **Static Assets**: CSS, images, PWA manifest

### Key Directories
- `Games/` - Main Blazor WebAssembly project
- `Games/Pages/` - Game components (Tamagotchi.razor, Tanks.razor with .cs/.css files)
- `Games/Services/` - Game logic (BattlefieldService.cs, PetGameService.cs)
- `Games/Models/` - Data models (GamepadModels.cs, PetState.cs, Tanks/TankModels.cs)
- `Games/wwwroot/js/` - JavaScript interop (gamepad.js, virtualjoystick.js)
- `Games/wwwroot/css/` - Styling and animations
- `Games/Layout/` - Application layout components (MainLayout.razor, NavMenu.razor)
- `Games/Interop/` - JavaScript interop definitions
- `.github/workflows/` - Azure Static Web Apps deployment pipeline

### Important Files
- `Games/Program.cs` - Application entry point, service registration
- `Games/Games.csproj` - Project file (targets .NET 9.0)
- `Games/App.razor` - Root component with routing
- `Games/Layout/NavMenu.razor` - Navigation between Pet and Tanks games
- `Games/wwwroot/index.html` - Main HTML page with JavaScript imports
- `Games/wwwroot/manifest.json` - PWA manifest for app installation
- `Games/wwwroot/service-worker.js` - Service worker for offline functionality

### File Relationships and Dependencies
- When modifying `BattlefieldService.cs`, check `Pages/Tanks.razor.cs` for integration points
- When modifying `PetGameService.cs`, check `Pages/Tamagotchi.razor.cs` for UI updates
- JavaScript files in `wwwroot/js/` provide gamepad input and canvas rendering
- CSS files provide responsive design and game-specific styling
- Service registration in `Program.cs` must match service injections in components
- Static assets in `wwwroot/` are copied to build output and cached by service worker

## Common Development Tasks

### Adding New Game Features
1. **Plan the change**: Identify which services, models, and UI components need updates
2. Update service classes in `Services/` for game logic
3. Modify corresponding `.razor.cs` files for UI integration
4. Update models in `Models/` if new data structures needed
5. Add/update CSS in component-specific `.razor.css` files for styling
6. Test with both mouse/touch and gamepad input if applicable
7. Run formatting: `dotnet format`
8. Build and test: `dotnet build Games.sln && dotnet run --project Games`
9. Validate with manual testing scenarios above

### UI Component Development
- **Razor components**: Use `@page` directive for routing, `@inject` for services
- **Code-behind**: Implement `ComponentBase`, use `OnInitialized()` and `OnAfterRenderAsync()`
- **JavaScript interop**: Use `IJSRuntime` for browser API calls
- **CSS isolation**: Use `.razor.css` files for component-specific styles
- **State management**: Call `StateHasChanged()` after async operations or state updates

### Game Logic Development
- **Services**: Keep game logic separate from UI in service classes
- **State management**: Use C# properties with private setters, expose via public methods
- **Performance**: Use `System.Timers.Timer` for game loops, optimize rendering calls
- **Input handling**: Abstract input sources (gamepad, touch, keyboard) into unified actions

### Debugging Issues
- **Browser dev tools**: F12 for JavaScript errors (gamepad/canvas issues)
- **Blazor debugging**: Use browser dev tools Console tab for .NET output
- **Hot reload**: `dotnet watch --project Games` for rapid iteration
- **Debugging hotkey**: Shift+Alt+D when app has focus for .NET debugging
- **Common issues**:
  - Missing PATH export for .NET commands
  - JavaScript interop failures: check browser console
  - Canvas rendering issues: verify element exists before JS calls
  - Service registration: ensure services are registered in `Program.cs`

### Performance Optimization
- **Rendering**: Minimize `StateHasChanged()` calls, use `shouldRender` overrides
- **Memory**: Dispose timers and event handlers in `IAsyncDisposable.DisposeAsync()`
- **JavaScript**: Cache DOM element references, use `requestAnimationFrame()` for smooth animation
- **CSS**: Use `transform` and `opacity` for animations (GPU-accelerated)

### Deployment
- **Automatic deployment**: Code deploys automatically via Azure Static Web Apps on push to `master` branch
- **Build process**: GitHub Actions builds and deploys to Azure (see `.github/workflows/`)
- **Build output**: Static files generated to `Games/bin/Debug/net9.0/wwwroot/` directory
- **No API backend**: Purely client-side games, all logic runs in browser via WebAssembly
- **PWA support**: App can be installed on mobile devices, works offline via service worker
- **CDN**: Deployed via Azure Static Web Apps with global CDN distribution

## Troubleshooting Guide

### Build Issues
- **"SDK not found"**: Ensure .NET 9.0 SDK is installed and PATH is exported
- **"Project file not found"**: Run commands from repository root directory
- **Restore failures**: Check internet connection, clear NuGet cache: `dotnet nuget locals all --clear`

### Runtime Issues
- **Blank page**: Check browser console for JavaScript errors, verify all assets loaded
- **Gamepad not working**: Ensure browser supports Gamepad API, check gamepad connection
- **Canvas not rendering**: Verify canvas element exists before JavaScript calls
- **Audio not playing**: Check browser autoplay policies, verify Web Audio API support

### Development Issues
- **Hot reload not working**: Restart `dotnet watch`, check file watchers aren't exhausted
- **CSS changes not visible**: Clear browser cache, check CSS isolation file naming
- **JavaScript interop failures**: Verify method names match between C# and JS, check browser console

### Common Error Messages
- **"Cannot read property of null"**: DOM element not found, add null checks in JavaScript
- **"Assembly not found"**: Missing package reference, check `.csproj` file
- **"Method not found"**: JavaScript function name mismatch, verify exact spelling and casing

## Technology Stack

### Core Technologies
- **Frontend Framework**: Blazor WebAssembly with .NET 9.0
- **UI Framework**: Bootstrap 5 for base styling, custom CSS for game aesthetics
- **Build System**: .NET SDK with MSBuild, supports hot reload and watch mode
- **Package Management**: NuGet for .NET packages, no npm/yarn dependencies

### Game Technologies
- **Input System**: Gamepad API via JavaScript interop, virtual joysticks for mobile
- **Graphics**: HTML5 Canvas with 2D rendering context for tank game
- **Audio**: Web Audio API for game sound effects and music
- **Animation**: CSS transforms and JavaScript `requestAnimationFrame()` for smooth 60fps

### Platform Features
- **PWA Support**: Service worker for offline functionality, app installation
- **Responsive Design**: Mobile-first approach, touch controls, adaptive layouts
- **Cross-Platform**: Runs on any modern browser with WebAssembly support
- **Performance**: Client-side execution, no server round-trips for game logic

### Development Tools
- **IDE Support**: Visual Studio, VS Code, JetBrains Rider
- **Debugging**: Browser dev tools integration, .NET debugging support
- **Code Quality**: Built-in formatting with `dotnet format`
- **Deployment**: Azure Static Web Apps with automated CI/CD

## Browser Compatibility

### Supported Browsers
- **Desktop**: Chrome 88+, Firefox 84+, Safari 14+, Edge 88+
- **Mobile**: iOS Safari 14+, Chrome Mobile 88+, Samsung Internet 13+
- **Requirements**: WebAssembly support, ES2018+ features, Gamepad API (optional)

### Feature Support
- **Core functionality**: Works on all supported browsers
- **Gamepad API**: Chrome, Firefox, Safari 14.1+, Edge (Xbox/PlayStation controllers)
- **Web Audio API**: All supported browsers (may require user interaction to start)
- **Service Worker**: All supported browsers for offline functionality
- **Canvas 2D**: Universal support across all browsers

### Mobile Considerations
- **Touch controls**: Virtual joysticks replace gamepad input on mobile
- **Performance**: Optimized for 60fps on modern mobile devices
- **Installation**: PWA can be installed as native app on iOS/Android
- **Orientation**: Responsive design adapts to portrait/landscape

## Best Practices

### Code Organization
- **Single Responsibility**: Each service handles one game system
- **Dependency Injection**: Use built-in DI container for service management
- **Async/Await**: Use async patterns for JavaScript interop and timers
- **Error Handling**: Wrap JavaScript interop calls in try-catch blocks

### Performance Guidelines
- **Minimize allocations**: Reuse objects where possible, avoid creating objects in game loops
- **Efficient rendering**: Only call `StateHasChanged()` when UI actually needs updates
- **JavaScript optimization**: Cache DOM references, use typed arrays for game data
- **CSS performance**: Use GPU-accelerated properties (transform, opacity) for animations

### Security Considerations
- **Client-side only**: No sensitive data processing (games are entertainment)
- **Content Security Policy**: Compatible with restrictive CSP headers
- **HTTPS required**: Service worker and some APIs require secure context

NEVER CANCEL long-running operations. Build and startup times are normal and expected.