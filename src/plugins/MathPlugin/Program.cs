using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MathPlugin;

[JsonSerializable(typeof(MathResponse))]
[JsonSerializable(typeof(ErrorResponse))]
internal partial class PluginJsonContext : JsonSerializerContext
{
}

public record MathResponse(int addition, int subtraction, int multiplication, double? division);
public record ErrorResponse(string error);

class Program
{
    static int Main(string[] args)
    {
        // CLI Mode: Handle request via args
        // Usage: MathPlugin <METHOD> <PATH>
        if (args.Length >= 2)
        {
            string method = args[0];
            string path = args[1];
            
            var response = HandleRequest(method, path);
            Console.WriteLine(response);
            return 0;
        }
        
        Console.WriteLine("{\"error\": \"Missing arguments\"}");
        return 1;
    }
    
    private static string HandleRequest(string method, string path)
    {
        if (method.ToUpper() == "GET" && path.StartsWith("/math/"))
        {
            var parts = path.Split('/');
            if (parts.Length >= 4 && int.TryParse(parts[2], out int a) && int.TryParse(parts[3], out int b))
            {
                var response = new MathResponse(
                    a + b,
                    a - b,
                    a * b,
                    b != 0 ? (double?)((double)a / b) : null
                );
                return JsonSerializer.Serialize(response, PluginJsonContext.Default.MathResponse);
            }
        }
        
        return JsonSerializer.Serialize(new ErrorResponse("Invalid request"), PluginJsonContext.Default.ErrorResponse);
    }
}