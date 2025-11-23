using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace wadoocore.HybridHost.IntegrationTests;

public class HybridHostTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HybridHostTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_MathPlugin_ReturnsCorrectResults()
    {
        // Arrange
        var client = _factory.CreateClient();
        var a = 10;
        var b = 2;

        // Act
        var response = await client.GetAsync($"/plugin/math?a={a}&b={b}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<MathResponse>();
        
        Assert.NotNull(result);
        Assert.Equal(12, result.addition);
        Assert.Equal(8, result.subtraction);
        Assert.Equal(20, result.multiplication);
        Assert.Equal(5, result.division);
    }

    [Fact]
    public async Task Get_MathPlugin_DivisionByZero_ReturnsNullForDivision()
    {
        // Arrange
        var client = _factory.CreateClient();
        var a = 10;
        var b = 0;

        // Act
        var response = await client.GetAsync($"/plugin/math?a={a}&b={b}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<MathResponse>();
        
        Assert.NotNull(result);
        Assert.Equal(10, result.addition);
        Assert.Equal(10, result.subtraction);
        Assert.Equal(0, result.multiplication);
        Assert.Null(result.division);
    }
    
    public record MathResponse(int addition, int subtraction, int multiplication, double? division);
}