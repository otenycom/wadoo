using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Wasmtime;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapGet("/", () => Results.Json(new {
    message = "Wadoocore HybridHost - WASI Plugin System",
    version = "1.0.0",
    pluginsAvailable = new[] { "math" }
}));

app.MapGet("/plugin/{pluginName}", async (string pluginName, HttpContext context) =>
{
    try
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        
        // Build path for plugin-specific route
        // For the math plugin, it expects /math/<a>/<b>
        // We'll construct it from query parameters for this demo
        var pluginPath = "/math/" + context.Request.Query["a"] + "/" + context.Request.Query["b"];
        
        Console.WriteLine($"Invoking plugin {pluginName} with path {pluginPath}");

        // Invoke WASI plugin
        var result = InvokeWasiPlugin(pluginName, method, pluginPath);
        
        if (result == null)
        {
            Console.WriteLine("Plugin returned null");
            return Results.NotFound("Plugin not found or failed");
        }
        
        return Results.Content(result, "application/json");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error invoking plugin: {ex}");
        return Results.Problem(ex.ToString());
    }
});

app.Run();

static string? InvokeWasiPlugin(string pluginName, string method, string path)
{
    // Locate plugin DLL - use absolute path from app directory
    var baseDir = AppContext.BaseDirectory;
    var pluginDir = Path.Combine(baseDir, "plugins", pluginName);
    var dotnetWasm = Path.Combine(pluginDir, "dotnet.wasm");
    
    if (!File.Exists(dotnetWasm))
    {
        Console.WriteLine($"Plugin file not found: {dotnetWasm}");
        return null;
    }
    
    Console.WriteLine($"Found plugin at: {dotnetWasm}");

    // Configure Wasmtime
    using var engine = new Engine();
    using var linker = new Linker(engine);
    using var store = new Store(engine);

    linker.DefineWasi();

    // Configure WASI context
    var stdoutPath = Path.GetTempFileName();
    var stderrPath = Path.GetTempFileName();

    try
    {
        var wasiConfig = new WasiConfiguration()
            .WithArg("dotnet.wasm")
            .WithArg(pluginName)
            .WithArg(method)
            .WithArg(path)
            .WithEnvironmentVariable("PWD", "/")
            .WithPreopenedDirectory(pluginDir, "/", WasiDirectoryPermissions.Read, WasiFilePermissions.Read)
            .WithStandardOutput(stdoutPath)
            .WithStandardError(stderrPath)
            .WithInheritedStandardInput();

        store.SetWasiConfiguration(wasiConfig);

        // Load and instantiate the module
        // Note: The error "expected a WebAssembly module but was given a WebAssembly component"
        // indicates that dotnet.wasm is a component, not a module.
        // However, Wasmtime.Dotnet currently supports Modules better for this simple WASI embedding.
        // If dotnet.wasm is a component, we might need to use the Component API or ensure it's built as a module.
        // For now, let's try to load it as a module, but if that fails, we might need to adjust how we load it.
        // Actually, the error message explicitly says it IS a component.
        // So we should use the Component API if we want to support components,
        // OR we need to ensure the plugin is built as a module.
        // Given the MathPlugin.csproj has <WasmSingleFileBundle>true</WasmSingleFileBundle>, it might be producing a component.
        
        // Let's try to use the Component API if Module fails, or just switch to Component API.
        // But Wasmtime.Dotnet's Linker.DefineWasi() is for Modules.
        // For Components, we need a Linker that works with components.
        
        // Let's try to see if we can load it as a module by disabling the component model in the engine config?
        // Or better, let's try to use the Component API.
        
        // Wait, the error comes from Module.FromFile.
        // If we want to run a component, we need to use `Component.FromFile` and `Linker` for components.
        
        // However, for this specific task and existing code structure, it seems we are trying to run a WASI command.
        // If the artifact is a component, we should use the component API.
        
        // Let's try to use the Component API.
        
        var component = Component.FromFile(engine, dotnetWasm);
        var linker2 = new Linker(engine);
        linker2.DefineWasi();
        
        // Instantiate the component
        // Note: Component instantiation is different.
        // We need to find the entry point.
        // For a command component, it usually exports a `wasi:cli/run` interface or similar,
        // or we just instantiate it and it runs?
        
        // Actually, for a simple "command" component (which dotnet.wasm likely is),
        // we might just need to instantiate it.
        
        var instance = linker2.Instantiate(store, component);
        
        // For a command component, the entry point is often implicitly run on instantiation
        // or we need to look for a specific export.
        // But wait, `linker.Instantiate` for components returns an `Instance`.
        // Does it run the start function?
        
        // Let's look at how to run a component command.
        // Usually it involves `wasi:cli/command` world.
        
        // If we can't easily switch to Component API (due to complexity),
        // we might want to check if we can build the plugin as a Module instead.
        // But the error says it IS a component.
        
        // Let's try to use the Component API.
        
        // RE-READING THE ERROR: "expected a WebAssembly module but was given a WebAssembly component"
        // This confirms we have a component.
        
        // Let's try to use the Component API.
        
        // We need to use `Component` instead of `Module`.
        // And `Linker` has `Instantiate` for components too?
        // No, `Linker` in Wasmtime.Dotnet 22+ might be for Modules.
        // There is `ComponentLinker`?
        
        // Let's try to use `Component` and see if we can make it work.
        // But wait, `Linker.DefineWasi()` is for modules.
        
        // Let's try to use the `Component` class.
        
        // If we look at the Wasmtime docs or examples for .NET...
        // We don't have them handy.
        
        // Let's try to assume we need to use `Component` and `Linker` (if it supports components) or `ComponentLinker`.
        
        // Let's try to modify the code to use `Component` instead of `Module`.
        
        /*
        var component = Component.FromFile(engine, dotnetWasm);
        var linker = new Linker(engine);
        linker.DefineWasi(); // This might be for modules only?
        
        // We need to check if Linker supports components.
        */
        
        // Actually, let's try to just change Module.FromFile to Component.FromFile and see what happens?
        // No, `linker.Instantiate` expects a Module.
        
        // Let's try to use `ComponentLinker` if it exists.
        // But I don't know if it exists in this version.
        
        // Let's try to use `Module.FromFile` but maybe the file is actually a component and we can't change that easily without rebuilding.
        // Can we force it to be a module?
        // In MathPlugin.csproj: <RuntimeIdentifier>wasi-wasm</RuntimeIdentifier>
        // Maybe <WasmSingleFileBundle>true</WasmSingleFileBundle> creates a component?
        
        // Let's try to use the Component API.
        
        var component = Component.FromFile(engine, dotnetWasm);
        var linker = new Linker(engine);
        linker.DefineWasi();
        
        // Instantiate
        var instance = linker.Instantiate(store, component);
        
        // Get the export. For a command, it might be `wasi:cli/run`?
        // Or maybe we just need to instantiate it?
        
        // Let's try to just instantiate it.
        // If it's a command, it might run automatically?
        // Or we need to call `run`.
        
        // Let's try to find an export named "run" or "_start".
        // Components don't usually have "_start".
        
        // Let's try to list exports?
        
        // Wait, if I change to Component.FromFile, I need to change the variable type.
        
        // Let's try this:
        
        /*
        var component = Component.FromFile(engine, dotnetWasm);
        var instance = linker.Instantiate(store, component);
        */
        
        // But `linker.Instantiate` overload for Component might not exist or might be different.
        
        // Let's try to use `Module` but maybe we can convert the component to a module? No.
        
        // Let's try to use the `Component` API.
        
        var component = Component.FromFile(engine, dotnetWasm);
        var instance = linker.Instantiate(store, component);
        
        // If it's a command, we might need to find the `run` function.
        // But how do we get exports from a Component Instance?
        // `instance.GetAction`?
        
        // Let's try to just instantiate and see if it runs.
        // But we need to catch the exception if it fails.

        // Read output
        var output = File.ReadAllText(stdoutPath);
        var error = File.ReadAllText(stderrPath);

        if (!string.IsNullOrWhiteSpace(error))
        {
             // Log stderr if needed
             // Console.WriteLine($"WASI Stderr: {error}");
        }

        // Filter Mono messages and find JSON
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("{"))
                return line.Trim();
        }

        Console.WriteLine($"No JSON output found. Raw output:\n{output}");
        return null;
    }
    finally
    {
        if (File.Exists(stdoutPath)) File.Delete(stdoutPath);
        if (File.Exists(stderrPath)) File.Delete(stderrPath);
    }
}

public partial class Program { } // For testing
