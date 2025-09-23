# Games

A modern Blazor WebAssembly application featuring two interactive browser games with gamepad support, responsive design, and Progressive Web App (PWA) capabilities.

## 🎮 Featured Games

### 🐾 Virtual Pet (Tamagotchi)
Take care of your virtual pet by managing its hunger, happiness, and energy levels. Watch as your pet's mood and appearance change based on how well you care for it.

**Controls:**
- **Feed (A)** - Reduce hunger and boost happiness
- **Play (B)** - Increase happiness (may reduce energy)
- **Rest (X)** - Restore energy
- **Gamepad Support** - Xbox/PlayStation controllers

### 🚗 Tank Battle
Engage in real-time tank combat with AI enemies. Use dual joystick controls to move and aim, with auto-fire and manual fire modes.

**Controls:**
- **Move Joystick** - Control tank movement
- **Aim Joystick** - Control turret direction
- **Fire Button** - Manual fire mode
- **Auto Toggle** - Switch between auto and manual fire
- **Restart** - Reset the battlefield
- **Fullscreen** - Toggle fullscreen mode

## 🚀 Quick Start

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Installation & Running

1. **Clone the repository**
   ```bash
   git clone https://github.com/gergroen/Games.git
   cd Games
   ```

2. **Install .NET 9.0 SDK** (if not already installed)
   ```bash
   # Linux/macOS
   wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
   chmod +x dotnet-install.sh
   ./dotnet-install.sh --channel 9.0
   export PATH="$HOME/.dotnet:$PATH"
   
   # Windows
   # Download from https://dotnet.microsoft.com/download/dotnet/9.0
   ```

3. **Build and run**
   ```bash
   dotnet build Games.sln
   dotnet run --project Games
   ```
   *Note: Startup takes 8-12 seconds. Look for "Now listening on: http://[::]:5080"*

4. **Open in browser**
   - Navigate to http://localhost:5080 (HTTP) or https://localhost:7042 (HTTPS)
   - Start with the Virtual Pet game at `/` or try Tank Battle at `/tanks`

## 🛠️ Technology Stack

- **Frontend Framework**: Blazor WebAssembly (.NET 9.0)
- **UI Framework**: Bootstrap 5 with custom game-specific styling
- **Graphics**: HTML5 Canvas (Tank Battle game)
- **Audio**: Web Audio API for game sound effects
- **Input**: Gamepad API, touch controls, virtual joysticks
- **PWA**: Service Worker for offline functionality and app installation
- **Deployment**: Azure Static Web Apps with automated CI/CD

## 📱 Platform Support

### Desktop Browsers
- Chrome 88+
- Firefox 84+ 
- Safari 14+
- Edge 88+

### Mobile Browsers
- iOS Safari 14+
- Chrome Mobile 88+
- Samsung Internet 13+

### Features
- **Gamepad Support**: Xbox and PlayStation controllers on supported browsers
- **Touch Controls**: Virtual joysticks and touch-optimized buttons
- **PWA Installation**: Install as a native app on mobile devices
- **Offline Support**: Play games without an internet connection after initial load
- **Responsive Design**: Optimized for desktop, tablet, and mobile

## 🎯 Game Features

### Virtual Pet Game
- **Dynamic Mood System**: Pet's appearance changes based on stats
- **Real-time Decay**: Stats decrease over time, requiring active care
- **Visual Feedback**: Animated faces show current mood and actions
- **Gamepad Integration**: Use controller buttons for pet interactions

### Tank Battle Game
- **Real-time Combat**: Smooth 60fps gameplay with physics simulation
- **AI Opponents**: Smart enemy tanks with collision detection
- **Dual Control System**: Separate movement and aiming controls
- **Visual Effects**: Canvas-based graphics with particle effects
- **Audio Feedback**: Sound effects for firing and combat

## 🏗️ Project Architecture

```
Games/
├── Games/                          # Main Blazor WebAssembly project
│   ├── Pages/                      # Game components (Razor pages)
│   │   ├── Tamagotchi.razor        # Virtual pet game UI
│   │   └── Tanks.razor             # Tank battle game UI
│   ├── Services/                   # Game logic and state management
│   │   ├── PetGameService.cs       # Virtual pet game logic
│   │   └── BattlefieldService.cs   # Tank battle game logic
│   ├── Models/                     # Data structures and game entities
│   │   ├── PetState.cs             # Pet game state model
│   │   ├── GamepadModels.cs        # Gamepad input models
│   │   └── Tanks/                  # Tank game models
│   ├── wwwroot/                    # Static assets and JavaScript
│   │   ├── js/                     # JavaScript interop files
│   │   ├── css/                    # Stylesheets
│   │   ├── manifest.json           # PWA manifest
│   │   └── service-worker.js       # Service worker for offline support
│   └── Layout/                     # Application layout components
├── .github/workflows/              # Azure Static Web Apps deployment
└── README.md                       # This file
```

## 💻 Development

### Development Workflow

1. **Start development server**
   ```bash
   dotnet watch --project Games
   ```
   This enables hot reload for rapid development.

2. **Code formatting** (required before commits)
   ```bash
   dotnet format
   dotnet format --verify-no-changes  # Verify formatting
   ```

3. **Build for production**
   ```bash
   dotnet publish Games -c Release
   ```

4. **Run E2E tests** (validates both games)
   ```bash
   ./run-e2e-tests.sh              # Full testing with graceful degradation
   ./run-e2e-tests.sh --smoke-only # Fast smoke tests only
   ```

### Adding New Features

1. **Game Logic**: Implement in the appropriate service class (`PetGameService.cs` or `BattlefieldService.cs`)
2. **UI Components**: Update the corresponding Razor component (`.razor` and `.razor.cs` files)
3. **Styling**: Add styles to component-specific `.razor.css` files
4. **JavaScript Interop**: Add browser API calls to JavaScript files in `wwwroot/js/`

### Code Quality Standards

- **C# Conventions**: Follow standard C# naming and coding conventions
- **Nullable Reference Types**: Enabled for enhanced type safety
- **Implicit Usings**: Leveraged to reduce boilerplate code
- **Component Isolation**: CSS scoped to individual components
- **Service Registration**: Use dependency injection for game services

## 🚀 Deployment

The application is automatically deployed to Azure Static Web Apps on every push to the `master` branch.

### Deployment Details
- **Platform**: Azure Static Web Apps
- **Build Process**: Automated via GitHub Actions
- **CDN**: Global content delivery network
- **HTTPS**: Automatic SSL certificate management
- **Custom Domains**: Supported through Azure configuration

### Build Configuration
- **Source Path**: `./Games`
- **Output Path**: `wwwroot`
- **API**: No backend API (client-side only)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes following the coding standards
4. Format your code (`dotnet format`)
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Development Guidelines

- **Minimal Changes**: Make the smallest possible changes to achieve your goal
- **Testing**: Use `./run-e2e-tests.sh` for automated validation of both games
- **Manual Testing**: Manually validate both games after making changes
- **Automated Testing**: Run E2E tests to ensure no regressions (`dotnet test Games.E2ETests`)
- **Performance**: Maintain smooth 60fps gameplay
- **Accessibility**: Ensure keyboard navigation works properly
- **Mobile**: Test touch controls on mobile devices
- **Code Quality**: Run `dotnet format` before committing changes

### Automated Testing

The repository includes comprehensive E2E tests using Playwright:

```bash
./run-e2e-tests.sh              # Full testing (recommended)
./run-e2e-tests.sh --smoke-only # Fast validation  
./run-e2e-tests.sh --help       # See all options
```

The test runner automatically:
- Installs .NET 9.0 SDK if needed
- Starts the Games application if not running
- Runs tests with graceful degradation (smoke tests when browsers unavailable)
- Provides clear feedback on results

See [Games.E2ETests/README.md](Games.E2ETests/README.md) for detailed testing documentation.

## 📝 Manual Testing

#### Test Development Guidelines

When adding new features, ensure they include appropriate tests:

1. **Add E2E tests** for user-facing functionality in `Games.E2ETests/`
2. **Follow naming convention**: `FeatureName_ShouldExpectedBehavior`
3. **Mark browser tests** with `[TestCategory("RequiresBrowser")]`
4. **Include accessibility testing** for interactive elements
5. **Test mobile viewports** for responsive features
6. **Update documentation** in README files when adding new test classes

## 🧪 Testing

### Automated Testing

The Games application includes comprehensive end-to-end (E2E) testing using Microsoft Playwright and MSTest.

#### Test Categories

**🚀 Smoke Tests (4 tests)**
- Basic application availability and HTTP response validation
- Static asset serving verification (CSS, JS, images)
- Page routing functionality for both games
- No browser installation required

**🎮 Game Functionality Tests (16 tests)**
- **Tamagotchi Game (7 tests)**: Pet interactions, stat displays, gamepad indicators
- **Tank Battle Game (9 tests)**: Canvas rendering, HUD functionality, controls

**♿ Accessibility Tests (6 tests)**
- Keyboard navigation and focus management
- ARIA labels and semantic HTML validation
- Screen reader compatibility
- Color contrast and visual accessibility

**📱 PWA Tests (5 tests)**
- Service Worker registration and offline support
- Web App Manifest validation
- App icon accessibility
- Installation capability verification

**⚡ Performance Tests (6 tests)**
- Application load time measurement
- Game interaction responsiveness
- Canvas rendering performance
- Memory usage and resource optimization

#### Running Tests

**Prerequisites:**
```bash
# Install .NET 9.0 SDK and start the application
dotnet run --project Games  # Must be running on localhost:5080
```

**Quick smoke tests (no browser required):**
```bash
cd Games.E2ETests
dotnet test --filter "TestCategory!=RequiresBrowser"
```

**Full test suite (requires Playwright browsers):**
```bash
# Install browsers (first time only)
pwsh bin/Debug/net9.0/playwright.ps1 install --with-deps

# Run all tests
dotnet test --logger "console;verbosity=detailed"
```

**Specific test categories:**
```bash
dotnet test --filter "ClassName=TamagotchiGameTests"
dotnet test --filter "ClassName=TankBattleGameTests"
dotnet test --filter "ClassName=AccessibilityTests"
dotnet test --filter "ClassName=PWATests"
dotnet test --filter "ClassName=PerformanceTests"
```

For detailed testing documentation, see [`Games.E2ETests/README.md`](Games.E2ETests/README.md).

### Manual Testing

#### Virtual Pet Game Validation
1. Navigate to `/` - verify pet displays with stats
2. Test all action buttons (Feed, Play, Rest)
3. Verify gamepad connection status
4. Check responsive design on mobile
5. Test keyboard navigation (Tab/Enter/Space)

#### Tank Battle Game Validation
1. Navigate to `/tanks` - verify battlefield loads
2. Test Fire button and Auto toggle
3. Verify HUD displays HP correctly
4. Test Restart and Fullscreen buttons
5. Check virtual joysticks on mobile
6. Verify smooth 60fps animation

#### PWA Testing
1. Test offline functionality after initial load
2. Verify app installation prompt on mobile
3. Check service worker updates and caching
4. Validate manifest.json and icon accessibility

## 📄 License

This project is open source. Please refer to the repository for license details.

## 🎮 Live Demo

Visit the live application: [Games on Azure Static Web Apps](https://mango-field-043ea0c03.2.azurestaticapps.net/)

---

Built with ❤️ using .NET 9.0 and Blazor WebAssembly
