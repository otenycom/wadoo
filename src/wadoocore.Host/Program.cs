using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Wasmtime;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapGet("/", () => Results.Json(new {
    message = "Wadoocore Host - WASI Plugin System",
    version = "1.0.0",
    pluginsAvailable = new[] { "math" }
}));

app.MapGet("/plugin/{pluginName}", async (string pluginName, HttpContext context) =>
{
    var method = context.Request.Method;
    var path = context.Request.Path.Value ?? "/";
    
    // Build path for plugin-specific route
    var pluginPath = "/math/" + context.Request.Query["a"] + "/" + context.Request.Query["b"];
    
    // Invoke WASI plugin
    var result = InvokeWasiPlugin(pluginName, method, pluginPath);
    
    if (result == null)
    {
        return Results.NotFound("Plugin not found or failed");
    }
    
    return Results.Content(result, "application/json");
});
    
    app.Run();
    
    static string? InvokeWasiPlugin(string pluginName, string method, string path)
    {
        // Locate plugin DLL - use absolute path from app directory
        var baseDir = AppContext.BaseDirectory;
        var pluginDir = Path.Combine(baseDir, "plugins", pluginName);
        var pluginDll = Path.Combine(pluginDir, $"{pluginName}.dll");
        var dotnetWasm = Path.Combine(pluginDir, "dotnet.wasm");
        
        if (!File.Exists(pluginDll) || !File.Exists(dotnetWasm))
        {
            Console.WriteLine($"Plugin files not found in: {pluginDir}");
            return null;
        }
    
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
            var module = Module.FromFile(engine, dotnetWasm);
            var instance = linker.Instantiate(store, module);
    
            // Run the _start function (entry point)
            var start = instance.GetAction("_start");
        if (start == null)
        {
            Console.WriteLine("Could not find _start function");
            return null;
        }

        try
        {
            start();
        }
        catch (WasmtimeException ex)
        {
            // Check if it's a normal exit (trap with exit code)
            // Wasmtime throws on exit(0) too sometimes depending on version/config,
            // but usually exit(0) is clean.
            // If it failed, we'll see it in stderr.
            Console.WriteLine($"Wasmtime execution error: {ex.Message}");
        }

        // Read output
        var output = File.ReadAllText(stdoutPath);
        var error = File.ReadAllText(stderrPath);

        if (!string.IsNullOrWhiteSpace(error))
        {
            // Mono writes some info to stderr even on success, so we check output first
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