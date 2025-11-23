using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Wadoocore;

[JsonSerializable(typeof(RootResponse))]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(InfoResponse))]
[JsonSerializable(typeof(MathResponse))]
[JsonSerializable(typeof(EchoResponse))]
[JsonSerializable(typeof(ErrorResponse))]
internal partial class AppJsonContext : JsonSerializerContext
{
}

public record RootResponse(string message, string version, string runtime, DateTime timestamp);
public record HealthResponse(string status, DateTime timestamp);
public record InfoResponse(string dotnetVersion, string runtimeIdentifier, string osDescription, string processArchitecture, DateTime currentTime);
public record MathResponse(int addition, int subtraction, int multiplication, double? division);
public record EchoResponse(string echo, DateTime timestamp);
public record ErrorResponse(string error, string? path = null, DateTime? timestamp = null);

/// <summary>
/// Simple HTTP request handler for WASI
/// </summary>
public class HttpRequestHandler
{
    public static string HandleRequest(string method, string path, string? body = null)
    {
        var response = method.ToUpper() switch
        {
            "GET" when path == "/" => GetRoot(),
            "GET" when path == "/health" => GetHealth(),
            "GET" when path == "/api/info" => GetInfo(),
            "GET" when path.StartsWith("/api/math/") => GetMath(path),
            "POST" when path == "/api/echo" => PostEcho(body),
            _ => NotFound(path)
        };
        
        return response;
    }
    
    private static string GetRoot()
    {
        var data = new RootResponse(
            "Hello from Wadoocore WASI!",
            Environment.Version.ToString(),
            System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier,
            DateTime.UtcNow
        );
        return JsonSerializer.Serialize(data, AppJsonContext.Default.RootResponse);
    }
    
    private static string GetHealth()
    {
        var data = new HealthResponse("healthy", DateTime.UtcNow);
        return JsonSerializer.Serialize(data, AppJsonContext.Default.HealthResponse);
    }
    
    private static string GetInfo()
    {
        var data = new InfoResponse(
            Environment.Version.ToString(),
            System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier,
            System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            DateTime.UtcNow
        );
        return JsonSerializer.Serialize(data, AppJsonContext.Default.InfoResponse);
    }
    
    private static string GetMath(string path)
    {
        try
        {
            var parts = path.Split('/');
            if (parts.Length >= 5 && int.TryParse(parts[3], out int a) && int.TryParse(parts[4], out int b))
            {
                var data = new MathResponse(
                    a + b,
                    a - b,
                    a * b,
                    b != 0 ? (double?)((double)a / b) : null
                );
                return JsonSerializer.Serialize(data, AppJsonContext.Default.MathResponse);
            }
        }
        catch { }
        
        return JsonSerializer.Serialize(new ErrorResponse("Invalid parameters"), AppJsonContext.Default.ErrorResponse);
    }
    
    private static string PostEcho(string? body)
    {
        var data = new EchoResponse(body ?? "", DateTime.UtcNow);
        return JsonSerializer.Serialize(data, AppJsonContext.Default.EchoResponse);
    }
    
    private static string NotFound(string path)
    {
        var data = new ErrorResponse("Not Found", path, DateTime.UtcNow);
        return JsonSerializer.Serialize(data, AppJsonContext.Default.ErrorResponse);
    }
}

class Program
{
    private static bool IsHttpMethod(string s) =>
        s.Equals("GET", StringComparison.OrdinalIgnoreCase) ||
        s.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
        s.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
        s.Equals("DELETE", StringComparison.OrdinalIgnoreCase);

    static async Task<int> Main(string[] args)
    {
        // Debug args
        // Console.WriteLine($"DEBUG: Args count: {args.Length}");
        // for (int i = 0; i < args.Length; i++) Console.WriteLine($"DEBUG: Arg[{i}]: {args[i]}");

        // CLI Mode: Handle a single request passed via arguments
        // Usage: wadoocore <METHOD> <PATH> [BODY]
        // Note: When running via wasmtime, the first argument might be the program name depending on how it's invoked.
        // We check if the first arg looks like an HTTP method.
        
        int startIndex = 0;
        if (args.Length > 0 && !IsHttpMethod(args[0]) && args.Length >= 3)
        {
            // Assume args[0] is the program name (e.g. "wadoocore")
            startIndex = 1;
        }

        if (args.Length >= startIndex + 2)
        {
            string method = args[startIndex];
            string path = args[startIndex + 1];
            string? body = args.Length > startIndex + 2 ? args[startIndex + 2] : null;
            
            if (IsHttpMethod(method))
            {
                var response = HttpRequestHandler.HandleRequest(method, path, body);
                Console.WriteLine(response);
                return 0;
            }
        }

        // Demo Mode: Run simulation
        Console.WriteLine("=== Wadoocore WASI HTTP Handler Demo ===");
        Console.WriteLine($"Running on .NET {Environment.Version}");
        Console.WriteLine($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier}");
        Console.WriteLine();
        
        Console.WriteLine("Simulating HTTP requests in WASI environment:");
        Console.WriteLine();
        
        // Simulate various HTTP requests
        Console.WriteLine("1. GET / - Root endpoint");
        var response1 = HttpRequestHandler.HandleRequest("GET", "/");
        Console.WriteLine($"   Response: {response1}");
        Console.WriteLine();
        
        Console.WriteLine("2. GET /health - Health check");
        var response2 = HttpRequestHandler.HandleRequest("GET", "/health");
        Console.WriteLine($"   Response: {response2}");
        Console.WriteLine();
        
        Console.WriteLine("3. GET /api/info - System information");
        var response3 = HttpRequestHandler.HandleRequest("GET", "/api/info");
        Console.WriteLine($"   Response: {response3}");
        Console.WriteLine();
        
        Console.WriteLine("4. GET /api/math/42/10 - Math operations");
        var response4 = HttpRequestHandler.HandleRequest("GET", "/api/math/42/10");
        Console.WriteLine($"   Response: {response4}");
        Console.WriteLine();
        
        Console.WriteLine("5. POST /api/echo - Echo endpoint");
        var response5 = HttpRequestHandler.HandleRequest("POST", "/api/echo", "Hello WASI!");
        Console.WriteLine($"   Response: {response5}");
        Console.WriteLine();
        
        Console.WriteLine("6. GET /unknown - Not found");
        var response6 = HttpRequestHandler.HandleRequest("GET", "/unknown");
        Console.WriteLine($"   Response: {response6}");
        Console.WriteLine();
        
        // await Task.Delay(100); // Task.Delay might not be supported in all WASI environments
        
        Console.WriteLine("=== HTTP Handler Demo Complete ===");
        Console.WriteLine();
        Console.WriteLine("Note: This demonstrates HTTP request handling in WASI.");
        Console.WriteLine("In production, this would be connected to a WASI HTTP host.");
        
        return 0;
    }
}