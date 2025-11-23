# Wadoocore Architecture Proposal

## Current State
The current project is a single WASI command-line app that simulates HTTP handling. Direct HTTP serving via `wasmtime serve` is experimental and limited.

## Proposed Architecture: Hybrid CoreCLR Host + WASI Plugins

### Overview
```
graph TD
    Client[Client HTTP Request] --> Host[ASP.NET Core Host<br/>(CoreCLR)]
    Host --> Router[Request Router]
    Router --> Middleware[Middleware Pipeline]
    Middleware --> PluginLoader[Plugin Loader<br/>(Wasmtime)]
    PluginLoader --> Plugin1[WASI Plugin 1<br/>(WASM Component)]
    PluginLoader --> Plugin2[WASI Plugin 2<br/>(WASM Component)]
    Plugin1 --> Response[Plugin Response]
    Plugin2 --> Response
    Response --> Middleware
    Middleware --> Client
```

### Components

1. **Host (CoreCLR)**:
   - ASP.NET Core 10 app
   - Handles TCP listening, routing, middleware
   - Stable, production-ready HTTP server

2. **Plugin Loader**:
   - Uses `Wasmtime` NuGet package
   - Loads/unloads WASI components dynamically
   - Manages plugin lifecycle

3. **WASI Plugins**:
   - .NET apps compiled to WASI WASM components
   - Implement WIT interface for request handling
   - Extensible, hot-swappable

### WIT Interface for Plugins
```
interface http-handler {
  handle: func(request: incoming-request) -> outgoing-response;
}
```

### Low Overhead Interop
- **Pinned Memory**: Share byte arrays between host and guest
- **Zero-Copy**: Avoid serialization where possible
- **Batch Processing**: Multiple requests per plugin call

### Bidirectional Communication
```
Host --> WASI: request data (bytes)
WASI --> Host: response data (bytes)
WASI --> Host: logging, metrics (via exported interfaces)
```

### Advantages
- ✅ **Stable Host**: ASP.NET Core production-ready
- ✅ **Dynamic Plugins**: Load/unload at runtime
- ✅ **Low Overhead**: Pinned memory interop
- ✅ **Extensible**: New plugins without host restart
- ✅ **Debuggable**: Host fully debuggable, plugins testable
- ✅ **Scalable**: Standard ASP.NET Core scaling

### Disadvantages
- ❌ **Not Pure WASI**: Host is CoreCLR
- ❌ **Dependency**: Wasmtime NuGet

## Alternative: Pure WASI Core

```
graph TD
    Client --> WasmtimeServe[wasmtime serve]
    WasmtimeServe --> CoreComponent[WASI Core Component]
    CoreComponent --> Plugin1[WASI Plugin 1]
    CoreComponent --> Plugin2[WASI Plugin 2]
```

**Status**: Experimental
- WASI HTTP 0.2.0 not stable
- Plugin composition complex
- Limited runtime support

## Recommendation

**Hybrid Architecture** - Best balance of stability and extensibility.

**Implementation Plan**:
1. Create `src/wadoocore.Host` ASP.NET Core project
2. Add Wasmtime NuGet package
3. Define WIT interface for plugins
4. Implement plugin loader
5. Migrate current logic to plugin
6. Add dynamic loading API
7. Update tests for new architecture

**Next Steps**: Approve architecture → Switch to code mode → Implement

**Questions**:
1. Prefer hybrid or pure WASI?
2. Specific plugin interfaces needed?
3. Deployment target (Kubernetes, serverless, etc.)?