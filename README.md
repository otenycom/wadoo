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

# Wadoo - .NET 10 WebAPI Host with Inline WASM Plugins

A complete .NET 10 solution demonstrating how to host inline WebAssembly plugins using **wasmtime.net v34** in a production-ready ASP.NET Core WebAPI.

## 🎯 What This Solution Provides

- **WebAPI Host** (`Wadoo.Host`) - ASP.NET Core 10 application that hosts WASM plugins in-process
- **WASM Plugin** (`MathPlugin`) - Simple WebAssembly module written in WAT format
- **Integration Tests** (`Wadoo.IntegrationTests`) - Comprehensive tests verifying the entire stack works

## 🏗️ Architecture

### In-Process Plugin Execution

This solution uses the **in-process approach** with wasmtime.net API:

```
┌─────────────────────────────────────────┐
│   ASP.NET Core WebAPI Host (CoreCLR)   │
│  ┌───────────────────────────────────┐  │
│  │    Wasmtime.Engine (v34.0.2)     │  │
│  │  ┌─────────────────────────────┐  │  │
│  │  │  MathPlugin.wasm (loaded)   │  │  │
│  │  │  - add(a, b) -> result      │  │  │
│  │  │  - subtract(a, b) -> result │  │  │
│  │  │  - multiply(a, b) -> result │  │  │
│  │  │  - divide(a, b) -> result   │  │  │
│  │  └─────────────────────────────┘  │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

**Key Benefits:**
- ✅ **Fast**: No process overhead, direct function calls
- ✅ **Simple**: Wasmtime.net handles all the complexity
- ✅ **Safe**: WASM sandbox provides security isolation
- ✅ **Scalable**: Each request gets its own Store (thread-safe)

## 📁 Project Structure

```
wadoo/
├── wadoo.sln                          # Solution file
├── global.json                        # .NET 10 SDK config
├── src/
│   ├── Wadoo.Host/                    # WebAPI Host
│   │   ├── Wadoo.Host.csproj         # References Wasmtime v34
│   │   ├── Program.cs                 # Host + WasmPluginService
│   │   └── appsettings.json
│   └── Plugins/
│       └── MathPlugin/                # WASM Plugin
│           ├── MathPlugin.csproj      # Simple library project
│           ├── math.wat               # WebAssembly Text Format
│           └── Program.cs             # (Not used in current impl)
└── tests/
    └── Wadoo.IntegrationTests/        # Integration Tests
        ├── Wadoo.IntegrationTests.csproj
        └── WadooHostIntegrationTests.cs
```

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (version 10.0.100+)
- No additional tools required (wasmtime.net is self-contained)

### Build & Test

```bash
# Restore packages
dotnet restore wadoo.sln

# Build all projects
dotnet build wadoo.sln

# Run integration tests (all 6 tests should pass)
dotnet test wadoo.sln
```

### Run the Host

```bash
# Start the WebAPI host
dotnet run --project src/Wadoo.Host/Wadoo.Host.csproj

# In another terminal, test the endpoints
curl http://localhost:5000/
curl http://localhost:5000/health
curl http://localhost:5000/api/math/add/5/3
```

## 📡 API Endpoints

### GET `/`
Returns welcome message with version info.

**Response:**
```json
{
  "message": "Wadoo WebAPI Host with inline WASM plugins",
  "version": "1.0.0",
  "timestamp": "2025-11-23T20:00:00Z"
}
```

### GET `/health`
Health check endpoint.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-11-23T20:00:00Z"
}
```

### GET `/api/math/add/{a}/{b}`
Calls the WASM plugin's `add` function.

**Example:** `/api/math/add/5/3`

**Response:**
```json
{
  "operation": "add",
  "a": 5,
  "b": 3,
  "result": 8
}
```

## 🔧 How It Works

### 1. WASM Plugin (math.wat)

The plugin is written in WebAssembly Text Format (WAT):

```wat
(module
  (func $add (export "add") (param $a i32) (param $b i32) (result i32)
    local.get $a
    local.get $b
    i32.add
  )
  ;; ... other functions ...
)
```

This gets loaded by wasmtime.net at runtime - **no separate compilation step needed!**

### 2. Host Integration (WasmPluginService)

The `WasmPluginService` manages the WASM lifecycle:

```csharp
public class WasmPluginService : IDisposable
{
    private readonly Engine _engine;    // Wasmtime engine
    private readonly Module _module;    // Loaded WASM module
    
    public WasmPluginService(...)
    {
        _engine = new Engine();
        _module = Module.FromTextFile(_engine, "math.wat");
    }
    
    public int CallMathPlugin(string operation, int a, int b)
    {
        using var store = new Store(_engine);
        using var linker = new Linker(_engine);
        
        var instance = linker.Instantiate(store, _module);
        var func = instance.GetFunction<int, int, int>(operation);
        
        return func(a, b);  // Direct function call!
    }
}
```

**Key Points:**
- `Engine` is thread-safe and reused
- `Store` is created per request (not thread-safe)
- `Module` is loaded once at startup
- Functions are called directly with type safety

### 3. ASP.NET Core Integration

The service is registered as a singleton:

```csharp
builder.Services.AddSingleton<WasmPluginService>();

app.MapGet("/api/math/add/{a}/{b}", (int a, int b, WasmPluginService pluginService) =>
{
    var result = pluginService.CallMathPlugin("add", a, b);
    return Results.Ok(new { operation = "add", a, b, result });
});
```

## 🧪 Testing

The solution includes comprehensive integration tests using `Microsoft.AspNetCore.Mvc.Testing`:

```csharp
public class WadooHostIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task MathPluginAdd_ReturnsCorrectSum()
    {
        var response = await _client.GetAsync("/api/math/add/5/3");
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadFromJsonAsync<MathResponse>();
        Assert.Equal(8, content.Result);
    }
}
```

**Test Results:**
```
Passed!  - Failed: 0, Passed: 6, Skipped: 0, Total: 6
```

## 📚 Wasmtime.NET Version

This solution uses **Wasmtime.NET v34.0.2** from NuGet:

```xml
<PackageReference Include="Wasmtime" Version="34.0.0" />
```

The actual installed version is 34.0.2 (latest compatible with 34.x).

## 🔑 Key Features

1. **Type-Safe Function Calls**: Use `GetFunction<int, int, int>()` for compile-time safety
2. **Automatic Plugin Discovery**: Service finds the WAT file by searching the source tree
3. **Error Handling**: Proper exceptions if plugin not found or function missing
4. **Logging**: Comprehensive logging via `ILogger<T>`
5. **Disposable Resources**: Proper cleanup of Engine and Module

## 🎓 Learning Resources

- [Wasmtime.NET Repository](https://github.com/bytecodealliance/wasmtime-dotnet)
- [WebAssembly Specification](https://webassembly.github.io/spec/)
- [WAT Format Documentation](https://developer.mozilla.org/en-US/docs/WebAssembly/Understanding_the_text_format)

## 🚧 Next Steps

To extend this solution:

1. **Add More Plugin Functions**: Extend `math.wat` with more operations
2. **Complex Types**: Explore memory passing for strings/arrays
3. **Multiple Plugins**: Load different WASM modules for different endpoints
4. **Hot Reload**: Implement plugin reloading without restarting the host
5. **Plugin Versioning**: Support multiple versions of the same plugin

## 📝 Notes

- The `Program.cs` file in MathPlugin is not currently used (legacy from initial WASI approach)
- Plugin path resolution searches upwards from the app directory for flexibility
- Each HTTP request creates a new `Store` for thread-safety
- The solution demonstrates pure in-process execution (no process spawning)

## ✅ Verification

To verify everything works:

```bash
# 1. Build
dotnet build wadoo.sln
# Should complete with 0 errors (warnings about Wasmtime version are OK)

# 2. Test
dotnet test wadoo.sln
# Should show: Passed: 6, Failed: 0

# 3. Run
dotnet run --project src/Wadoo.Host/Wadoo.Host.csproj
# Should start listening on http://localhost:5000

# 4. Test endpoint
curl http://localhost:5000/api/math/add/10/20
# Should return: {"operation":"add","a":10,"b":20,"result":30}
```

All 6 integration tests passing confirms:
- ✅ Host starts correctly
- ✅ Plugin loads successfully  
- ✅ Functions are called correctly
- ✅ Results are returned properly
- ✅ Multiple operations work
- ✅ Edge cases handled (negative numbers, zero, large numbers)

---

**Built with .NET 10 and Wasmtime.NET v34** 🚀