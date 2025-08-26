# Games Repository Instructions

Games is a Blazor WebAssembly application featuring two interactive browser games: a Tamagotchi-style virtual pet game and a real-time tank battle game. Both games support gamepad input alongside mouse/touch controls.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

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

### Build and Run Commands
- **Clean build**: `dotnet clean Games.sln && dotnet build Games.sln` -- takes 4-6 seconds. NEVER CANCEL. Set timeout to 30+ seconds.
- **Quick build**: `dotnet build Games.sln` -- takes 2-4 seconds when no changes to dependencies.
- **Run application**: `dotnet run --project Games` -- startup takes 8-12 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
  - Application runs on: http://localhost:5080 (HTTP) and https://localhost:7042 (HTTPS)
  - Look for: "Now listening on: http://[::]:5080" to confirm successful startup

### Code Quality
- **Format code**: `dotnet format` -- takes 5-10 seconds. NEVER CANCEL. Set timeout to 30+ seconds.
- **Verify formatting**: `dotnet format --verify-no-changes` -- takes 2-3 seconds. Must pass before committing.
- No unit tests exist in this project (games are interaction-heavy and tested manually).

## Validation Scenarios

**ALWAYS manually validate any code changes by running both games through these scenarios:**

### Tamagotchi Game Validation (Default route: `/`)
1. Navigate to root page - should display pet with stats (Hunger, Happiness, Energy, Mood)
2. Click "Feed (A)" button - hunger should decrease, happiness may increase, pet face should change
3. Click "Play (B)" button - happiness should increase, energy may decrease
4. Click "Rest (X)" button - energy should increase
5. Verify pet's appearance and mood change based on stats
6. Check gamepad connection status is displayed

### Tanks Game Validation (Route: `/tanks`)
1. Navigate to `/tanks` - should display battlefield with player tank (red) and enemy tank (green)
2. Verify HUD shows "Player HP: 100 | Enemy HP: 100"
3. Click "FIRE" button - should trigger combat, HP values may change
4. Click "Restart" button - should reset both HP to 100 and respawn tanks
5. Test "AUTO OFF"/"AUTO ON" toggle button functionality
6. Verify virtual joysticks are visible on screen
7. Check for game sounds when firing (audio is generated via Web Audio API)

### Performance Validation
- Both games should maintain smooth animation (60fps target)
- Canvas should resize properly when browser window changes
- Mobile touch controls should work on virtual joysticks

## Repository Structure

### Key Directories
- `Games/` - Main Blazor WebAssembly project
- `Games/Pages/` - Game components (Tamagotchi.razor, Tanks.razor with .cs/.css files)
- `Games/Services/` - Game logic (BattlefieldService.cs, PetGameService.cs)
- `Games/Models/` - Data models (GamepadModels.cs, PetState.cs, Tanks/TankModels.cs)
- `Games/wwwroot/js/` - JavaScript interop (gamepad.js, virtualjoystick.js)
- `Games/wwwroot/css/` - Styling and animations
- `.github/workflows/` - Azure Static Web Apps deployment pipeline

### Important Files
- `Games/Program.cs` - Application entry point, service registration
- `Games/Games.csproj` - Project file (targets .NET 9.0)
- `Games/App.razor` - Root component with routing
- `Games/Layout/NavMenu.razor` - Navigation between Pet and Tanks games
- `Games/wwwroot/index.html` - Main HTML page with JavaScript imports

### File Relationships
- When modifying `BattlefieldService.cs`, check `Pages/Tanks.razor.cs` for integration points
- When modifying `PetGameService.cs`, check `Pages/Tamagotchi.razor.cs` for UI updates
- JavaScript files in `wwwroot/js/` provide gamepad input and canvas rendering
- CSS files provide responsive design and game-specific styling

## Common Development Tasks

### Adding New Game Features
1. Update service classes in `Services/` for game logic
2. Modify corresponding `.razor.cs` files for UI integration
3. Update models in `Models/` if new data structures needed
4. Test with both mouse/touch and gamepad input if applicable
5. Run formatting: `dotnet format`
6. Validate with manual testing scenarios above

### Debugging Issues
- Use browser dev tools for JavaScript errors (gamepad/canvas issues)
- Check browser console for .NET debugging output
- Use "Debugging hotkey: Shift+Alt+D" when app has focus for .NET debugging
- Verify .NET 9.0 SDK is properly installed if build fails

### Deployment
- Code deploys automatically via Azure Static Web Apps on push to `master` branch
- Build output goes to `Games/wwwroot/` directory
- No API backend required - purely client-side games

## Technology Stack
- **Frontend**: Blazor WebAssembly with .NET 9.0
- **UI**: Bootstrap for base styling, custom CSS for game aesthetics
- **Input**: Gamepad API via JavaScript interop, virtual joysticks for mobile
- **Graphics**: HTML5 Canvas with 2D context for tank game rendering
- **Audio**: Web Audio API for game sound effects
- **Deployment**: Azure Static Web Apps with automated CI/CD

## Browser Compatibility
- Supports modern browsers with WebAssembly support
- Gamepad API works in Chrome, Firefox, Safari, Edge
- Touch controls work on mobile devices
- Responsive design adapts to different screen sizes

NEVER CANCEL long-running operations. Build and startup times are normal and expected.