using System;
using System.Text.Json;
using Xunit;

namespace Wadoocore.Tests;

public class HttpHandlerTests
{
    [Fact]
    public void GetRoot_ShouldReturnWelcomeMessage()
    {
        // Act
        var response = HttpRequestHandler.HandleRequest("GET", "/");
        var data = JsonSerializer.Deserialize<JsonElement>(response);
        
        // Assert
        Assert.NotNull(response);
        Assert.Contains("Hello from Wadoocore WASI!", data.GetProperty("message").GetString());
        Assert.NotNull(data.GetProperty("version").GetString());
        Assert.NotNull(data.GetProperty("runtime").GetString());
    }
    
    [Fact]
    public void GetHealth_ShouldReturnHealthStatus()
    {
        // Act
        var response = HttpRequestHandler.HandleRequest("GET", "/health");
        var data = JsonSerializer.Deserialize<JsonElement>(response);
        
        // Assert
        Assert.Equal("healthy", data.GetProperty("status").GetString());
        Assert.NotEqual(default(DateTime), data.GetProperty("timestamp").GetDateTime());
    }
    
    [Fact]
    public void GetInfo_ShouldReturnSystemInformation()
    {
        // Act
        var response = HttpRequestHandler.HandleRequest("GET", "/api/info");
        var data = JsonSerializer.Deserialize<JsonElement>(response);
        
        // Assert
        Assert.NotNull(data.GetProperty("dotnetVersion").GetString());
        Assert.NotNull(data.GetProperty("runtimeIdentifier").GetString());
        Assert.NotNull(data.GetProperty("osDescription").GetString());
        Assert.NotNull(data.GetProperty("processArchitecture").GetString());
    }
    
    [Theory]
    [InlineData("/api/math/10/5", 15, 5, 50, 2.0)]
    [InlineData("/api/math/100/10", 110, 90, 1000, 10.0)]
    [InlineData("/api/math/7/3", 10, 4, 21, 2.333)]
    public void GetMath_ShouldPerformCorrectCalculations(string path, int expectedAdd, int expectedSub, int expectedMul, double expectedDiv)
    {
        // Act
        var response = HttpRequestHandler.HandleRequest("GET", path);
        var data = JsonSerializer.Deserialize<JsonElement>(response);
        
        // Assert
        Assert.Equal(expectedAdd, data.GetProperty("addition").GetInt32());
        Assert.Equal(expectedSub, data.GetProperty("subtraction").GetInt32());
        Assert.Equal(expectedMul, data.GetProperty("multiplication").GetInt32());
        
        var division = data.GetProperty("division").GetDouble();
        Assert.InRange(division, expectedDiv - 0.01, expectedDiv + 0.01);
    }
    
    [Fact]
    public void GetMath_WithZeroDivisor_ShouldHandleGracefully()
    {
        // Act
        var response = HttpRequestHandler.HandleRequest("GET", "/api/math/10/0");
        var data = JsonSerializer.Deserialize<JsonElement>(response);
        
        // Assert
        Assert.Equal(10, data.GetProperty("addition").GetInt32());
        Assert.Equal(10, data.GetProperty("subtraction").GetInt32());
        Assert.Equal(0, data.GetProperty("multiplication").GetInt32());
        Assert.Equal(JsonValueKind.Null, data.GetProperty("division").ValueKind);
    }
    
    [Fact]
    public void PostEcho_ShouldEchoRequestBody()
    {
        // Arrange
        string testBody = "Hello WASI World!";
        
        // Act
        var response = HttpRequestHandler.HandleRequest("POST", "/api/echo", testBody);
        var data = JsonSerializer.Deserialize<JsonElement>(response);
        
        // Assert
        Assert.Equal(testBody, data.GetProperty("echo").GetString());
        Assert.NotEqual(default(DateTime), data.GetProperty("timestamp").GetDateTime());
    }
    
    [Fact]
    public void PostEcho_WithEmptyBody_ShouldReturnEmptyEcho()
    {
        // Act
        var response = HttpRequestHandler.HandleRequest("POST", "/api/echo", "");
        var data = JsonSerializer.Deserialize<JsonElement>(response);
        
        // Assert
        Assert.Equal("", data.GetProperty("echo").GetString());
    }
    
    [Fact]
    public void PostEcho_WithNullBody_ShouldReturnEmptyEcho()
    {
        // Act
        var response = HttpRequestHandler.HandleRequest("POST", "/api/echo", null);
        var data = JsonSerializer.Deserialize<JsonElement>(response);
        
        // Assert
        Assert.Equal("", data.GetProperty("echo").GetString());
    }
    
    [Theory]
    [InlineData("GET", "/unknown")]
    [InlineData("POST", "/notfound")]
    [InlineData("DELETE", "/api/info")]
    public void UnknownEndpoint_ShouldReturnNotFound(string method, string path)
    {
        // Act
        var response = HttpRequestHandler.HandleRequest(method, path);
        var data = JsonSerializer.Deserialize<JsonElement>(response);
        
        // Assert
        Assert.Equal("Not Found", data.GetProperty("error").GetString());
        Assert.Equal(path, data.GetProperty("path").GetString());
    }
    
    [Fact]
    public void HandleRequest_ShouldBeCaseInsensitiveForMethod()
    {
        // Act
        var responseUpper = HttpRequestHandler.HandleRequest("GET", "/");
        var responseLower = HttpRequestHandler.HandleRequest("get", "/");
        var responseMixed = HttpRequestHandler.HandleRequest("Get", "/");
        
        var dataUpper = JsonSerializer.Deserialize<JsonElement>(responseUpper);
        var dataLower = JsonSerializer.Deserialize<JsonElement>(responseLower);
        var dataMixed = JsonSerializer.Deserialize<JsonElement>(responseMixed);
        
        // Assert - Compare structure and message, not exact timestamp
        Assert.Equal(dataUpper.GetProperty("message").GetString(), dataLower.GetProperty("message").GetString());
        Assert.Equal(dataUpper.GetProperty("message").GetString(), dataMixed.GetProperty("message").GetString());
        Assert.Equal(dataUpper.GetProperty("version").GetString(), dataLower.GetProperty("version").GetString());
        Assert.Equal(dataUpper.GetProperty("runtime").GetString(), dataMixed.GetProperty("runtime").GetString());
    }
    
    [Fact]
    public void GetMath_WithInvalidPath_ShouldReturnError()
    {
        // Act
        var response = HttpRequestHandler.HandleRequest("GET", "/api/math/invalid");
        var data = JsonSerializer.Deserialize<JsonElement>(response);
        
        // Assert
        Assert.True(data.TryGetProperty("error", out _));
    }
    
    [Fact]
    public void AllEndpoints_ShouldReturnValidJson()
    {
        // Arrange
        var endpoints = new[]
        {
            ("GET", "/"),
            ("GET", "/health"),
            ("GET", "/api/info"),
            ("GET", "/api/math/5/3"),
            ("POST", "/api/echo")
        };
        
        // Act & Assert
        foreach (var (method, path) in endpoints)
        {
            var response = HttpRequestHandler.HandleRequest(method, path, "test");
            
            // Should not throw when deserializing
            var data = JsonSerializer.Deserialize<JsonElement>(response);
            Assert.NotEqual(JsonValueKind.Undefined, data.ValueKind);
        }
    }
}