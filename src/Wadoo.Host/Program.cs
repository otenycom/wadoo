using Wasmtime;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddSingleton<WasmPluginService>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapGet("/", () => new
{
    message = "Wadoo WebAPI Host with inline WASM plugins",
    version = "1.0.0",
    timestamp = DateTime.UtcNow
});

app.MapGet("/health", () => new
{
    status = "healthy",
    timestamp = DateTime.UtcNow
});

app.MapGet("/api/math/add/{a}/{b}", (int a, int b, WasmPluginService pluginService) =>
{
    try
    {
        var result = pluginService.CallMathPlugin("add", a, b);
        return Results.Ok(new { operation = "add", a, b, result });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error calling math plugin: {ex.Message}");
    }
});

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }

/// <summary>
/// Service that loads and manages WASM plugins using in-process wasmtime.net API
/// </summary>
public class WasmPluginService : IDisposable
{
    private readonly Engine _engine;
    private readonly Module _module;
    private readonly ILogger<WasmPluginService> _logger;

    public WasmPluginService(IConfiguration configuration, ILogger<WasmPluginService> logger)
    {
        _logger = logger;
        
        // Create wasmtime engine
        _engine = new Engine();
        
        // Get plugin path from configuration or find it
        var pluginPath = configuration["PluginPath"];
        
        if (string.IsNullOrEmpty(pluginPath))
        {
            // Try to find the plugin by searching upwards from the current directory
            var currentDir = AppContext.BaseDirectory;
            var pluginFileName = "math.wat";
            
            // Search for the plugin in the source tree
            for (int i = 0; i < 10; i++)
            {
                var testPath = Path.Combine(currentDir, "src", "Plugins", "MathPlugin", pluginFileName);
                if (File.Exists(testPath))
                {
                    pluginPath = testPath;
                    break;
                }
                
                var parentDir = Directory.GetParent(currentDir);
                if (parentDir == null) break;
                currentDir = parentDir.FullName;
            }
            
            if (string.IsNullOrEmpty(pluginPath))
            {
                throw new FileNotFoundException($"Could not find {pluginFileName} in the source tree. Searched from: {AppContext.BaseDirectory}");
            }
        }
        
        _logger.LogInformation("Loading plugin from: {PluginPath}", pluginPath);
        
        if (!File.Exists(pluginPath))
        {
            throw new FileNotFoundException($"Plugin not found at: {pluginPath}");
        }

        // Load the module from WAT file (wasmtime.net can parse WAT directly)
        _module = Module.FromTextFile(_engine, pluginPath);
        
        _logger.LogInformation("Plugin loaded successfully");
    }

    public int CallMathPlugin(string operation, int a, int b)
    {
        // Create a new store for each invocation (stores are not thread-safe)
        using var store = new Store(_engine);
        
        // Create a linker (no imports needed for our simple math module)
        using var linker = new Linker(_engine);
        
        // Instantiate the module
        var instance = linker.Instantiate(store, _module);
        
        // Get the function based on operation
        var functionName = operation.ToLowerInvariant();
        var func = instance.GetFunction<int, int, int>(functionName);
        
        if (func == null)
        {
            throw new InvalidOperationException($"Function '{functionName}' not found in plugin");
        }
        
        // Call the function and return result
        var result = func(a, b);
        
        _logger.LogDebug("Called {Function}({A}, {B}) = {Result}", functionName, a, b, result);
        
        return result;
    }

    public void Dispose()
    {
        _module?.Dispose();
        _engine?.Dispose();
    }
}