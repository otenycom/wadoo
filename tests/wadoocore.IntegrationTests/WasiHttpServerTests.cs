using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Wadoocore.IntegrationTests;

public class WasiHttpServerTests : IDisposable
{
    private readonly HttpListener _listener;
    private readonly string _url;
    private readonly HttpClient _client;
    private readonly Task _serverTask;
    private bool _isRunning;
    private readonly string _wasiAppPath;
    private readonly string _wasmtimePath;

    public WasiHttpServerTests()
    {
        // Find a free port
        var port = new Random().Next(5000, 8000);
        _url = $"http://localhost:{port}/";
        
        _listener = new HttpListener();
        _listener.Prefixes.Add(_url);
        _listener.Start();
        _isRunning = true;
        
        _client = new HttpClient {
            BaseAddress = new Uri(_url),
            Timeout = TimeSpan.FromMinutes(2) // Increase timeout for WASI startup
        };
        
        // Locate the WASI app artifacts
        // Assuming we are running from tests/wadoocore.IntegrationTests/bin/Debug/net10.0/
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        _wasiAppPath = Path.Combine(solutionRoot, "src/wadoocore/bin/Debug/net10.0/wasi-wasm/AppBundle");
        
        // Check if wasmtime is available
        _wasmtimePath = "wasmtime"; // Assume in PATH
        
        _serverTask = Task.Run(RunServer);
    }

    private async Task RunServer()
    {
        while (_isRunning)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                // Process request in background to allow concurrent requests
                _ = Task.Run(() => ProcessRequest(context));
            }
            catch (HttpListenerException) when (!_isRunning)
            {
                // Server stopped
            }
            catch (ObjectDisposedException) when (!_isRunning)
            {
                // Server stopped
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex}");
            }
        }
    }

    private async Task ProcessRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            string method = request.HttpMethod;
            string path = request.Url?.PathAndQuery ?? "/";
            string body = "";

            if (request.HasEntityBody)
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                body = await reader.ReadToEndAsync();
            }

            // Invoke WASI app
            var output = await RunWasiApp(method, path, body);

            // Write response
            byte[] buffer = Encoding.UTF8.GetBytes(output);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "application/json";
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            byte[] buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { error = ex.Message }));
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }
        finally
        {
            response.Close();
        }
    }

    private async Task<string> RunWasiApp(string method, string path, string body)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _wasmtimePath,
            WorkingDirectory = _wasiAppPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Arguments to run the WASI app with HTTP support enabled
        // wasmtime run -S http --dir .::/ --env PWD=/ dotnet.wasm wadoocore <METHOD> <PATH> [BODY]
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("-S");
        psi.ArgumentList.Add("http");
        psi.ArgumentList.Add("--dir");
        psi.ArgumentList.Add(".::/");
        psi.ArgumentList.Add("--env");
        psi.ArgumentList.Add("PWD=/");
        psi.ArgumentList.Add("dotnet.wasm");
        psi.ArgumentList.Add("wadoocore");
        psi.ArgumentList.Add(method);
        psi.ArgumentList.Add(path);
        if (!string.IsNullOrEmpty(body))
        {
            psi.ArgumentList.Add(body);
        }

        using var process = Process.Start(psi);
        if (process == null) throw new Exception("Failed to start wasmtime");

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"WASI app failed (Exit Code: {process.ExitCode}): {error}");
        }

        // Filter out Mono runtime messages (lines starting with /__w/)
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            // Skip Mono messages and empty lines
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("/") || line.StartsWith("[MONO]"))
                continue;

            // Return the first line that looks like JSON
            if (line.TrimStart().StartsWith("{") || line.TrimStart().StartsWith("["))
            {
                return line.Trim();
            }
        }

        // If no JSON found, throw with the whole output for debugging
        throw new Exception($"No JSON output found. Raw output:\n{output}");
    }

    [Fact]
    public async Task GetRoot_ReturnsWelcomeMessage()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Server returned {response.StatusCode}: {content}");
        }
        
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        
        Assert.Equal("Hello from Wadoocore WASI!", root.GetProperty("message").GetString());
        Assert.Equal("wasi-wasm", root.GetProperty("runtime").GetString());
    }

    [Fact]
    public async Task GetMath_ReturnsCorrectCalculation()
    {
        var response = await _client.GetAsync("/api/math/10/2");
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        
        Assert.Equal(12, root.GetProperty("addition").GetInt32());
        Assert.Equal(5, root.GetProperty("division").GetDouble());
    }

    [Fact]
    public async Task PostEcho_ReturnsBody()
    {
        var payload = "Integration Test Payload";
        var response = await _client.PostAsync("/api/echo", new StringContent(payload));
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        
        Assert.Equal(payload, root.GetProperty("echo").GetString());
    }

    public void Dispose()
    {
        _isRunning = false;
        _listener.Stop();
        _listener.Close();
        _client.Dispose();
    }
}