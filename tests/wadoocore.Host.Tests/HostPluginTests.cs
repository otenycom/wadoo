using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Wadoocore.Host.Tests;

public class HostPluginTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HostPluginTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        
        // Copy plugin to test output directory
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var sourcePluginDir = Path.Combine(solutionRoot, "src/wadoocore.Host/plugins/math");
        var targetPluginDir = Path.Combine(AppContext.BaseDirectory, "plugins/math");
        
        if (Directory.Exists(sourcePluginDir))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetPluginDir)!);
            CopyDirectory(sourcePluginDir, targetPluginDir);
        }
    }
    
    private static void CopyDirectory(string source, string destination)
    {
        if (Directory.Exists(destination))
            Directory.Delete(destination, true);
            
        Directory.CreateDirectory(destination);
        
        foreach (var file in Directory.GetFiles(source))
        {
            var fileName = Path.GetFileName(file);
            File.Copy(file, Path.Combine(destination, fileName), true);
        }
    }

    [Fact]
    public async Task GetRoot_ReturnsHostInfo()
    {
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.Equal("Wadoocore Host - WASI Plugin System", data.GetProperty("message").GetString());
    }

    [Fact]
    public async Task GetPluginMath_ReturnsCalculation()
    {
        var response = await _client.GetAsync("/plugin/math?a=10&b=5");
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Request failed: {response.StatusCode} {error}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.Equal(15, data.GetProperty("addition").GetInt32());
        Assert.Equal(5, data.GetProperty("subtraction").GetInt32());
    }
}