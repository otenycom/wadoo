# Wadoocore

A best-practice .NET 10 project targeting WASI (WebAssembly System Interface) with HTTP request handling capabilities.

This project demonstrates a WASI-compatible HTTP request handler with comprehensive testing, showcasing how to build and test .NET applications for WebAssembly environments.

## Features

- ✨ HTTP request handler with multiple endpoints
- 🧪 Comprehensive test suite (29 tests)
- 🚀 WASI/WebAssembly compatible
- 📦 .NET 10 with latest features
- 🔧 VSCode integration with tasks and debugging

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- VSCode with C# extension
- [wasmtime](https://wasmtime.dev/) (for running WASM modules)

## Quick Setup for New Developers

1. Run the setup script for your platform:
   - macOS/Linux: `./setup.sh`
   - Windows: `.\setup.ps1`

The setup script will:
- Install the `wasi-experimental` workload if missing
- Restore dependencies
- Build the project
- Run the application demo

## Project Structure

```
wadoocore/
├── global.json          # Pins .NET SDK version to 10.0.100
├── wadoocore.sln        # Solution file
├── src/
│   ├── wadoocore/       # Original WASI CLI app
│   │   ├── wadoocore.csproj
│   │   └── Program.cs   # HTTP handler with CLI and demo modes
│   ├── wadoocore.Host/  # NEW: CoreCLR ASP.NET host for plugins
│   │   ├── wadoocore.Host.csproj
│   │   └── Program.cs   # Host server that loads WASI plugins
│   └── plugins/
│       └── MathPlugin/  # Sample WASI plugin
│           ├── MathPlugin.csproj
│           └── Program.cs
├── tests/
│   ├── wadoocore.Tests/          # Unit tests
│   ├── wadoocore.IntegrationTests/ # CLI integration tests
│   └── wadoocore.Host.Tests/     # Host plugin tests
├── docs/
│   ├── WASI-DEBUGGING.md
│   └── ARCHITECTURE.md   # Hybrid architecture design
├── run-wasi.sh
└── .vscode/
```

## Architecture

**Hybrid: CoreCLR Host + WASI Plugins**

The project now supports two deployment models:

### 1. **Standalone WASI App** (`src/wadoocore`)
- Runs directly via `wasmtime` with `-S http` flag
- CLI mode for single requests
- Demo mode for testing
- ✅ 32 passing tests (unit + integration)

### 2. **Hybrid Host** (`src/wadoocore.Host`) **[NEW]**
- **Host**: CoreCLR ASP.NET Core 10 server
- **Plugins**: WASI WASM modules loaded dynamically
- **Communication**: Process-based invocation (CGI-like)
- **Advantages**:
  - ✅ Stable, production-ready HTTP server (ASP.NET Core)
  - ✅ Dynamic plugin loading/unloading
  - ✅ Full debugging support for host
  - ✅ Extensible architecture

**Current Implementation**: Plugins are invoked as separate `wasmtime` processes for each request (CGI model). Future optimization: In-process Wasmtime API for lower overhead.

## HTTP Endpoints

The application provides the following HTTP request handlers:

### GET /
Returns a welcome message with system information.
```json
{
  "message": "Hello from Wadoocore WASI!",
  "version": "10.0.0",
  "runtime": "wasi-wasm",
  "timestamp": "2025-11-23T15:00:00Z"
}
```

### GET /health
Health check endpoint.
```json
{
  "status": "healthy",
  "timestamp": "2025-11-23T15:00:00Z"
}
```

### GET /api/info
Detailed system information.
```json
{
  "dotnetVersion": "10.0.0",
  "runtimeIdentifier": "wasi-wasm",
  "osDescription": "WASI",
  "processArchitecture": "Wasm",
  "currentTime": "2025-11-23T15:00:00Z"
}
```

### GET /api/math/{a}/{b}
Performs mathematical operations on two integers.
```json
{
  "addition": 15,
  "subtraction": 5,
  "multiplication": 50,
  "division": 2.0
}
```

### POST /api/echo
Echoes the request body back.
```json
{
  "echo": "Your message here",
  "timestamp": "2025-11-23T15:00:00Z"
}
```

## Building and Running

### Build the Project
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```
All 29 tests should pass:
- 13 basic WASI functionality tests
- 16 HTTP handler tests

### Run the Demo Application

**Option 1: Run via Tests (Recommended for Development)**
```bash
# All 29 tests pass and demonstrate full functionality
dotnet test

# See the HTTP handler in action through test output
dotnet test --logger "console;verbosity=detailed"
```

**Option 2: Run on WASI (Experimental)**
We have provided a script to run the application on `wasmtime` with the necessary flags to support WASI HTTP 0.2.0.

```bash
# Run the WASI application
./run-wasi.sh
```

This script:
1. Builds the project
2. Copies necessary dependencies (including ICU data)
3. Runs `wasmtime` with `-S http` to enable HTTP support
4. Maps directories correctly for execution

**Note:** Direct execution via `dotnet run` will fail because it doesn't pass the required `-S http` flag to the runtime. Use `./run-wasi.sh` instead.

### Publish to WebAssembly
```bash
dotnet publish src/wadoocore/wadoocore.csproj -r wasi-wasm -c Release -o publish-wasm
```

### Run with wasmtime (Advanced)
Note: Native HTTP server functionality in WASI is still experimental. The current implementation demonstrates HTTP request handling logic that can be integrated with WASI HTTP hosts.

```bash
wasmtime run publish-wasm/dotnet.wasm publish-wasm/wadoocore.dll
```

## Development

### Using VSCode Tasks

- **Build**: `Ctrl+Shift+B` or Command Palette > `Tasks: Run Build Task`
- **Run**: Command Palette > `Tasks: Run Task` > `run`
- **Test**: `dotnet test` in terminal
- **Debug**: `F5` (uses launch.json configuration)
**Integration Tests** ([`WasiHttpServerTests.cs`](tests/wadoocore.IntegrationTests/WasiHttpServerTests.cs:1))
- Runs the actual WASI application using `wasmtime`
- Starts a local HTTP server to proxy requests to the WASI app
- Verifies end-to-end functionality of the WASI binary
- Demonstrates "serving" the API via WASI (simulated)

### Architecture of Integration Tests

The integration tests use a **CGI-like architecture** to simulate a WASI HTTP server:

1.  **Test Host (CLR)**: The test starts a standard .NET `HttpListener` on localhost.
2.  **Proxy Logic**: When a request is received, the test invokes the WASI binary via `wasmtime` as a command-line process.
3.  **WASI Execution**: The WASI app runs, processes the request (passed via CLI args), prints the JSON response to stdout, and exits.
4.  **Response**: The test captures the output and returns it as the HTTP response.

**Note:** The WASI binary itself is **not** opening a socket or listening on a port. It is running as a short-lived command for each request. This demonstrates how WASI applications can be used in serverless or CGI environments.


### Debugging Tests in VSCode

The project includes debug configurations in [`.vscode/launch.json`](.vscode/launch.json:1):

#### Method 1: Using C# Extension Test Explorer (Recommended)
1. Install the **C# Dev Kit** extension in VSCode (if not already installed)
2. Open the Testing sidebar (beaker icon) or press `Ctrl+Shift+T`
3. You'll see all your tests listed hierarchically
4. Set breakpoints in your test code
5. **Right-click** on any test → **Debug Test**
6. The debugger will stop at your breakpoints!

#### Method 2: Using F5 Debug Configuration
1. Set breakpoints in your test files ([`HttpHandlerTests.cs`](tests/wadoocore.Tests/HttpHandlerTests.cs:1) or [`WasiBasicTests.cs`](tests/wadoocore.Tests/WasiBasicTests.cs:1))
2. Press `F5` → Select **"Debug Tests"**
3. Wait for the debugger to attach (you'll see a message in the Debug Console)
4. Tests will run and stop at breakpoints

**IMPORTANT**: When using "Debug Tests" configuration, the first time you run it:
- The Debug Console will show: `Waiting for debugger to attach...`
- You may need to wait a few seconds for the test host to initialize
- Subsequent runs will be faster

#### Method 3: Command Line (Alternative)
```bash
# In terminal, run with debugger environment variable set
VSTEST_HOST_DEBUG=1 dotnet test tests/wadoocore.Tests/wadoocore.Tests.csproj
```
Then attach the VSCode debugger when prompted.

**Debug Tips:**
- **F10** - Step over
- **F11** - Step into
- **Shift+F11** - Step out
- **F5** - Continue
- View variables in the Debug sidebar
- Use the Debug Console for immediate expressions
- Check the Debug Console for test output and messages

**Troubleshooting:**
- If breakpoints show as hollow circles (not filled), the debugger hasn't attached yet
- Make sure to select "Debug Tests" from the debug configuration dropdown
- Wait for the "Waiting for debugger..." message in Debug Console
- The first debug session may take 10-15 seconds to attach

### Can I Debug WASI/WebAssembly?

**Short answer:** Debug the tests instead! They run on regular .NET with full debugging support.

**Why this works:** The [`HttpRequestHandler`](src/wadoocore/Program.cs:11) class contains pure C# logic that works identically on both regular .NET and WASI. By writing comprehensive tests ([`HttpHandlerTests.cs`](tests/wadoocore.Tests/HttpHandlerTests.cs:1)), you can:
- ✅ Debug with full VSCode features (breakpoints, variable inspection, etc.)
- ✅ Iterate quickly without WASM compilation
- ✅ Catch bugs before deploying to WASI

**For WASI-specific debugging:** See the comprehensive guide in [`docs/WASI-DEBUGGING.md`](docs/WASI-DEBUGGING.md:1) which covers:
- Current state of WASI debugging
- wasmtime debugging capabilities
- Console.WriteLine debugging
- Recommended development workflow

**TL;DR:** The test-driven approach gives you production-grade debugging while WASI debugging standards mature.

### Adding New Endpoints

1. Add a new handler method in [`HttpRequestHandler`](src/wadoocore/Program.cs:11) class
2. Update the [`HandleRequest`](src/wadoocore/Program.cs:14) method to route to your handler
3. Add corresponding tests in [`HttpHandlerTests.cs`](tests/wadoocore.Tests/HttpHandlerTests.cs:1)
4. Run tests to verify: `dotnet test`

## Testing

The project includes comprehensive testing:

### Test Categories

**Basic WASI Tests** ([`WasiBasicTests.cs`](tests/wadoocore.Tests/WasiBasicTests.cs:1))
- String operations
- DateTime operations
- Math operations
- Collection operations (List, LINQ, Dictionary)
- Async/await functionality
- Runtime information

**HTTP Handler Tests** ([`HttpHandlerTests.cs`](tests/wadoocore.Tests/HttpHandlerTests.cs:1))
- All endpoint responses
- JSON serialization
- Error handling
- Edge cases (null/empty inputs)
- Case-insensitive method handling

### Running Specific Tests
```bash
# Run all tests
dotnet test

# Run only HTTP handler tests
dotnet test --filter HttpHandlerTests

# Run only basic WASI tests
dotnet test --filter WasiBasicTests

# Run with detailed output
dotnet test -v detailed
```

## Technical Details

- **SDK**: Microsoft.NET.Sdk
- **Target Framework**: net10.0
- **Runtime Identifier**: wasi-wasm
- **Trimming**: Enabled for size optimization
- **Test Framework**: xUnit 2.9.2
- **JSON Serialization**: System.Text.Json (built-in)

## Next Steps

- Integrate with a WASI HTTP host for true server functionality
- Add more complex request handling (headers, query parameters)
- Implement authentication/authorization
- Add OpenAPI/Swagger documentation
- Deploy to WASM-compatible hosting platforms

## Troubleshooting

### wasmtime errors about HTTP types
The WASI HTTP specification is still evolving. The current implementation demonstrates HTTP handler logic that works in .NET but may need updates as WASI HTTP matures.

### Tests failing
Ensure you have the latest .NET 10 SDK installed:
```bash
dotnet --version  # Should show 10.0.100 or later
```

### Build warnings about IL2026
These are trimming analyzer warnings about JSON serialization. They're expected and can be ignored for this demonstration project.

## Contributing

Contributions are welcome! Please ensure all tests pass before submitting pull requests.

## License

This project is provided as-is for educational and demonstration purposes.

