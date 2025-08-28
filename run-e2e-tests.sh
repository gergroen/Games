#!/bin/bash

# E2E Test Runner for Games Application
# This script provides automated E2E testing with graceful degradation for coding copilot and CI environments

set -e  # Exit on any error in main commands, but we'll handle specific failures gracefully

# Configuration
GAMES_URL="http://localhost:5080"
GAMES_PROJECT="Games"
E2E_PROJECT="Games.E2ETests"
MAX_APP_START_WAIT=30
MAX_BROWSER_INSTALL_WAIT=300
VERBOSE=${VERBOSE:-false}

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_verbose() {
    if [ "$VERBOSE" = true ]; then
        echo -e "${BLUE}[VERBOSE]${NC} $1"
    fi
}

# Function to check if .NET 9.0 is available
check_dotnet() {
    log_info "Checking .NET 9.0 SDK availability..."
    
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET CLI not found in PATH"
        return 1
    fi
    
    local version=$(dotnet --version)
    log_verbose "Found .NET version: $version"
    
    if [[ ! "$version" =~ ^9\. ]]; then
        log_warning "Expected .NET 9.x, found: $version"
        log_info "Attempting to install .NET 9.0 SDK..."
        
        if [ -f "./dotnet-install.sh" ]; then
            chmod +x ./dotnet-install.sh
            ./dotnet-install.sh --channel 9.0
            export PATH="$HOME/.dotnet:$PATH"
        else
            log_error "dotnet-install.sh not found. Please install .NET 9.0 SDK manually."
            return 1
        fi
    fi
    
    log_success ".NET 9.0 SDK is available"
    return 0
}

# Function to check if the Games application is running
check_app_running() {
    log_verbose "Checking if Games application is running on $GAMES_URL..."
    
    if curl -s -f "$GAMES_URL" > /dev/null 2>&1; then
        log_success "Games application is running on $GAMES_URL"
        return 0
    else
        log_info "Games application is not running on $GAMES_URL"
        return 1
    fi
}

# Function to start the Games application
start_games_app() {
    log_info "Starting Games application..."
    
    # Build the application first
    log_verbose "Building Games application..."
    if ! dotnet build "$GAMES_PROJECT" --verbosity quiet; then
        log_error "Failed to build Games application"
        return 1
    fi
    
    # Start the application in background
    log_verbose "Starting Games application in background..."
    nohup dotnet run --project "$GAMES_PROJECT" > games-app.log 2>&1 &
    local app_pid=$!
    echo $app_pid > games-app.pid
    
    # Wait for the application to start
    log_info "Waiting for Games application to start (max ${MAX_APP_START_WAIT}s)..."
    local count=0
    while [ $count -lt $MAX_APP_START_WAIT ]; do
        if check_app_running; then
            log_success "Games application started successfully (PID: $app_pid)"
            return 0
        fi
        sleep 1
        count=$((count + 1))
        if [ $((count % 5)) -eq 0 ]; then
            log_verbose "Still waiting... (${count}s elapsed)"
        fi
    done
    
    log_error "Games application failed to start within ${MAX_APP_START_WAIT} seconds"
    log_info "Check games-app.log for details"
    return 1
}

# Function to stop the Games application
stop_games_app() {
    if [ -f games-app.pid ]; then
        local pid=$(cat games-app.pid)
        log_info "Stopping Games application (PID: $pid)..."
        if kill $pid 2>/dev/null; then
            log_success "Games application stopped"
        else
            log_warning "Games application may have already stopped"
        fi
        rm -f games-app.pid
    fi
}

# Function to build E2E tests
build_e2e_tests() {
    log_info "Building E2E tests..."
    if dotnet build "$E2E_PROJECT" --verbosity quiet; then
        log_success "E2E tests built successfully"
        return 0
    else
        log_error "Failed to build E2E tests"
        return 1
    fi
}

# Function to install Playwright browsers with retry logic
install_playwright_browsers() {
    log_info "Attempting to install Playwright browsers..."
    
    cd "$E2E_PROJECT"
    
    # Try multiple installation strategies
    local strategies=(
        "pwsh bin/Debug/net9.0/playwright.ps1 install --with-deps"
        "pwsh bin/Debug/net9.0/playwright.ps1 install chromium"
        "dotnet tool install --global Microsoft.Playwright.CLI && playwright install chromium"
    )
    
    for strategy in "${strategies[@]}"; do
        log_verbose "Trying: $strategy"
        
        # Use timeout to prevent hanging
        if timeout $MAX_BROWSER_INSTALL_WAIT bash -c "$strategy" > playwright-install.log 2>&1; then
            log_success "Playwright browsers installed successfully"
            cd ..
            return 0
        else
            log_warning "Installation strategy failed: $strategy"
            log_verbose "Check playwright-install.log for details"
        fi
    done
    
    log_error "All Playwright browser installation strategies failed"
    log_info "Will run smoke tests only"
    cd ..
    return 1
}

# Function to run smoke tests
run_smoke_tests() {
    log_info "Running smoke tests (no browser required)..."
    
    if dotnet test "$E2E_PROJECT" --filter "TestCategory!=RequiresBrowser" --logger "console;verbosity=minimal" --no-build; then
        log_success "Smoke tests passed"
        return 0
    else
        log_error "Smoke tests failed"
        return 1
    fi
}

# Function to run full E2E tests
run_full_e2e_tests() {
    log_info "Running full E2E tests (including browser tests)..."
    
    if dotnet test "$E2E_PROJECT" --logger "console;verbosity=minimal" --no-build; then
        log_success "All E2E tests passed"
        return 0
    else
        log_error "Some E2E tests failed"
        return 1
    fi
}

# Function to run tests with graceful degradation
run_tests_with_degradation() {
    local smoke_result=0
    local browser_result=0
    local browsers_available=false
    
    # Always run smoke tests first
    if ! run_smoke_tests; then
        smoke_result=1
        log_error "Smoke tests failed - basic application functionality is broken"
    fi
    
    # Try to install browsers and run full tests
    if install_playwright_browsers; then
        browsers_available=true
        if ! run_full_e2e_tests; then
            browser_result=1
        fi
    else
        log_warning "Skipping browser tests due to installation failure"
        browser_result=1
    fi
    
    # Summary
    echo
    log_info "=== E2E Test Results Summary ==="
    
    if [ $smoke_result -eq 0 ]; then
        log_success "✓ Smoke tests: PASSED"
    else
        log_error "✗ Smoke tests: FAILED"
    fi
    
    if [ "$browsers_available" = true ]; then
        if [ $browser_result -eq 0 ]; then
            log_success "✓ Browser tests: PASSED"
        else
            log_error "✗ Browser tests: FAILED"
        fi
    else
        log_warning "◯ Browser tests: SKIPPED (browsers not available)"
    fi
    
    # Return appropriate exit code
    if [ $smoke_result -eq 0 ]; then
        if [ "$browsers_available" = true ] && [ $browser_result -eq 0 ]; then
            log_success "All tests completed successfully"
            return 0
        elif [ "$browsers_available" = false ]; then
            log_warning "Tests completed with degraded coverage (smoke tests only)"
            return 0
        else
            log_error "Tests completed with failures"
            return 1
        fi
    else
        log_error "Critical test failures detected"
        return 1
    fi
}

# Function to show usage
show_usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Run E2E tests for the Games application with automatic setup and graceful degradation."
    echo ""
    echo "Options:"
    echo "  --smoke-only     Run only smoke tests (no browser installation)"
    echo "  --verbose        Enable verbose output"
    echo "  --help           Show this help message"
    echo ""
    echo "Environment Variables:"
    echo "  VERBOSE=true     Enable verbose output"
    echo ""
    echo "Examples:"
    echo "  $0                    # Run all tests with graceful degradation"
    echo "  $0 --smoke-only       # Run only smoke tests"
    echo "  VERBOSE=true $0       # Run with verbose output"
}

# Main execution function
main() {
    local smoke_only=false
    local started_app=false
    
    # Parse command line arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --smoke-only)
                smoke_only=true
                shift
                ;;
            --verbose)
                VERBOSE=true
                shift
                ;;
            --help)
                show_usage
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                show_usage
                exit 1
                ;;
        esac
    done
    
    # Set up cleanup trap
    trap 'stop_games_app; exit' INT TERM EXIT
    
    log_info "Starting E2E test runner for Games application..."
    
    # Check prerequisites
    if ! check_dotnet; then
        log_error "Cannot proceed without .NET 9.0 SDK"
        exit 1
    fi
    
    # Check if app is running, start if needed
    if ! check_app_running; then
        if ! start_games_app; then
            log_error "Cannot proceed without Games application running"
            exit 1
        fi
        started_app=true
    fi
    
    # Build E2E tests
    if ! build_e2e_tests; then
        log_error "Cannot proceed without building E2E tests"
        exit 1
    fi
    
    # Run tests based on mode
    if [ "$smoke_only" = true ]; then
        log_info "Running in smoke-only mode"
        if run_smoke_tests; then
            log_success "Smoke tests completed successfully"
            exit 0
        else
            log_error "Smoke tests failed"
            exit 1
        fi
    else
        if run_tests_with_degradation; then
            exit 0
        else
            exit 1
        fi
    fi
}

# Execute main function with all arguments
main "$@"