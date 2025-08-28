# E2E Test Runner

This script provides automated end-to-end testing for the Games application with graceful degradation for coding copilot and CI environments.

## Quick Start

```bash
# Run E2E tests (recommended)
./run-e2e-tests.sh

# Fast smoke tests only
./run-e2e-tests.sh --smoke-only

# Help
./run-e2e-tests.sh --help
```

## What It Does

1. **Checks Prerequisites**: Verifies .NET 9.0 SDK is installed
2. **Manages Application**: Starts Games app if not running
3. **Builds Tests**: Compiles the E2E test project
4. **Graceful Testing**: Runs appropriate tests based on environment capabilities
5. **Clean Reporting**: Provides clear success/failure status

## Expected Results

- ✅ **Success**: All tests pass
- ⚠️ **Degraded**: Smoke tests pass, browser tests skipped (acceptable)
- ❌ **Failure**: Smoke tests fail (requires attention)

See [Games.E2ETests/README.md](Games.E2ETests/README.md) for detailed documentation.