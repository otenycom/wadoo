using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Wadoo.IntegrationTests;

public class WadooHostIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WadooHostIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRoot_ReturnsWelcomeMessage()
    {
        // Act
        var response = await _client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<RootResponse>();
        
        Assert.NotNull(content);
        Assert.Equal("Wadoo WebAPI Host with inline WASM plugins", content.Message);
        Assert.Equal("1.0.0", content.Version);
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        
        Assert.NotNull(content);
        Assert.Equal("healthy", content.Status);
    }

    [Fact]
    public async Task MathPluginAdd_ReturnsCorrectSum()
    {
        // Arrange
        int a = 5;
        int b = 3;
        int expectedResult = 8;

        // Act
        var response = await _client.GetAsync($"/api/math/add/{a}/{b}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<MathResponse>();
        
        Assert.NotNull(content);
        Assert.Equal("add", content.Operation);
        Assert.Equal(a, content.A);
        Assert.Equal(b, content.B);
        Assert.Equal(expectedResult, content.Result);
    }

    [Fact]
    public async Task MathPluginAdd_WithNegativeNumbers_ReturnsCorrectSum()
    {
        // Arrange
        int a = -10;
        int b = 15;
        int expectedResult = 5;

        // Act
        var response = await _client.GetAsync($"/api/math/add/{a}/{b}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<MathResponse>();
        
        Assert.NotNull(content);
        Assert.Equal(expectedResult, content.Result);
    }

    [Fact]
    public async Task MathPluginAdd_WithZero_ReturnsCorrectSum()
    {
        // Arrange
        int a = 0;
        int b = 42;
        int expectedResult = 42;

        // Act
        var response = await _client.GetAsync($"/api/math/add/{a}/{b}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<MathResponse>();
        
        Assert.NotNull(content);
        Assert.Equal(expectedResult, content.Result);
    }

    [Fact]
    public async Task MathPluginAdd_WithLargeNumbers_ReturnsCorrectSum()
    {
        // Arrange
        int a = 1000000;
        int b = 2000000;
        int expectedResult = 3000000;

        // Act
        var response = await _client.GetAsync($"/api/math/add/{a}/{b}");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<MathResponse>();
        
        Assert.NotNull(content);
        Assert.Equal(expectedResult, content.Result);
    }
}

// Response DTOs
public record RootResponse(string Message, string Version, DateTime Timestamp);
public record HealthResponse(string Status, DateTime Timestamp);
public record MathResponse(string Operation, int A, int B, int Result);