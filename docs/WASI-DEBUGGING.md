# WASI/WebAssembly Debugging Guide

## Current Debugging Capabilities

### ✅ What Works Well: Test-Driven Development

**The tests in this project run on regular .NET** (not WASI), which means:
- ✨ Full debugging support with breakpoints
- 🔍 Variable inspection works perfectly
- 🚀 Fast debug cycles
- 💯 All VSCode debugging features available

This is the **recommended development workflow**:
1. Write your logic in [`Program.cs`](../src/wadoocore/Program.cs:1)
2. Create tests in [`tests/wadoocore.Tests/`](../tests/wadoocore.Tests/)
3. Debug tests with full VSCode support
4. Once working, compile to WASI for deployment

### ⚠️ WASI Debugging Limitations

Debugging the actual WASI/WebAssembly binary has these constraints:

#### Current State (2025)
- **Source-level debugging**: Limited/Experimental
- **wasmtime debugging**: Basic support via DWARF debug info
- **VSCode integration**: Requires experimental extensions
- **Commercial maturity**: Not production-ready yet

#### Why It's Limited
1. WebAssembly debugging standards are still evolving
2. Debug information in WASM binaries is optional
3. Host implementations (wasmtime, wasmer) have varying support
4. VSCode extensions for WASM debugging are experimental

## Debugging Options

### Option 1: Test on Regular .NET (Recommended ✅)

**Pros:**
- Full debugging support
- Fast iteration
- All VSCode features work
- Industry standard approach

**How:**
```bash
# Write tests, set breakpoints, debug with F5
dotnet test --filter YourTestName
```

The [`HttpRequestHandler`](../src/wadoocore/Program.cs:11) class is pure C# logic that works identically on both regular .NET and WASI.

### Option 2: Console.WriteLine Debugging

Add logging to your WASI application:

```csharp
Console.WriteLine($"DEBUG: Variable value = {myVar}");
```

Run and view output:
```bash
dotnet run --project src/wadoocore
```

### Option 3: Wasmtime DWARF Debugging (Advanced)

wasmtime has experimental debugging support:

#### Prerequisites
```bash
# Install latest wasmtime
curl https://wasmtime.dev/install.sh -sSf | bash

# Verify version (needs 15.0+)
wasmtime --version
```

#### Build with Debug Info
```bash
# Publish with debug symbols
dotnet publish src/wadoocore/wadoocore.csproj \
  -r wasi-wasm \
  -c Debug \
  -p:WasmDebugLevel=1 \
  -o publish-wasm
```

#### Run with Debugger
```bash
# Run with DWARF debugging
wasmtime run --invoke _start \
  --debug-info \
  publish-wasm/dotnet.wasm publish-wasm/wadoocore.dll
```

**Limitations:**
- Limited variable inspection
- No step-through debugging in VSCode
- Mostly useful for crash analysis
- Requires understanding of WASM internals

### Option 4: WASM DevTools (Browser-based)

If your WASI app runs in a browser context:
1. Use Chrome/Edge DevTools
2. WASM debugging panel (experimental)
3. Source maps for WASM modules

**Note:** Our current project targets WASI (not browser WASM), so this doesn't apply.

## Recommended Workflow

```
┌─────────────────────────────────────┐
│ 1. Write Logic in Program.cs       │
│    (HttpRequestHandler class)       │
└─────────────┬───────────────────────┘
              │
              ▼
┌─────────────────────────────────────┐
│ 2. Write Tests                      │
│    (HttpHandlerTests.cs)            │
└─────────────┬───────────────────────┘
              │
              ▼
┌─────────────────────────────────────┐
│ 3. Debug Tests with Breakpoints     │
│    ✅ Full VSCode debugging         │
│    ✅ Variable inspection           │
│    ✅ Fast iteration                │
└─────────────┬───────────────────────┘
              │
              ▼
┌─────────────────────────────────────┐
│ 4. Tests Pass?                      │
├────────────┬────────────────────────┤
│   Yes      │   No                   │
│    ▼       │    └──────────┐        │
│    │       └───────────────┼───┐    │
│    │                       │   │    │
│    │       ┌───────────────┘   │    │
│    │       │                   │    │
│    │       ▼                   │    │
│    │  Fix bugs with debugger   │    │
│    │  (back to step 3) ────────┘    │
│    │                                │
│    ▼                                │
│ Compile to WASI                     │
│ dotnet publish -r wasi-wasm         │
└─────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────┐
│ 5. Deploy WASM Binary               │
│    (Deployment ready!)              │
└─────────────────────────────────────┘
```

## Practical Example

Let's say you want to debug the math endpoint:

### Good Approach ✅
```csharp
// In HttpHandlerTests.cs
[Fact]
public void GetMath_DebugExample()
{
    // Set breakpoint on next line
    var response = HttpRequestHandler.HandleRequest("GET", "/api/math/10/5");
    
    // Step through HandleRequest method
    // Inspect variables
    // Verify logic
    
    var data = JsonSerializer.Deserialize<JsonElement>(response);
    Assert.Equal(15, data.GetProperty("addition").GetInt32());
}
```

Press F5 → "Debug Tests" → Breakpoint hits → Debug normally!

### Running on WASI (Experimental) ✅
We have provided a script `run-wasi.sh` that successfully runs the application on `wasmtime` by enabling the necessary HTTP features.

```bash
./run-wasi.sh
```

This works by:
1. Using `wasmtime run -S http` to enable WASI HTTP 0.2.0 support.
2. Correctly mapping directories so the runtime can find `icudt.dat` and assemblies.
3. Using source-generated JSON serialization to avoid reflection issues.

## Future of WASI Debugging

The WebAssembly ecosystem is rapidly evolving:

**Expected improvements:**
- Better DWARF support in wasmtime
- VSCode extensions for WASM debugging
- Source-level debugging with breakpoints
- Remote debugging protocols

**Current best practice:**
- Test on regular .NET with full debugging
- Compile to WASI for deployment
- Use Console.WriteLine for WASI-specific issues

## Additional Resources

- [wasmtime debugging docs](https://docs.wasmtime.dev/cli-install.html)
- [WASM debugging spec](https://webassembly.github.io/debugging/)
- [.NET WASI support](https://github.com/dotnet/runtime/blob/main/src/mono/wasi/README.md)

## Summary

**For 99% of development**: Debug your tests on regular .NET ✅

**For WASI-specific issues**: Use logging and wasmtime debugging ⚠️

The test-driven approach in this project gives you:
- ✅ Full debugging capabilities
- ✅ Fast development cycles  
- ✅ High confidence before WASI compilation
- ✅ Production-ready workflow

WASI debugging will improve over time, but the test-driven approach will always be the fastest and most reliable way to develop WebAssembly applications.