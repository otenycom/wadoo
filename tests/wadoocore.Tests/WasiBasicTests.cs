using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Wadoocore.Tests;

public class WasiBasicTests
{
    [Fact]
    public void StringOperations_ShouldWork()
    {
        // Arrange
        string greeting = "Hello from WASI!";
        
        // Act & Assert
        Assert.NotNull(greeting);
        Assert.Equal("Hello from WASI!", greeting);
        Assert.Contains("WASI", greeting);
    }
    
    [Fact]
    public void DateTimeOperations_ShouldWork()
    {
        // Arrange & Act
        DateTime now = DateTime.UtcNow;
        DateTime today = DateTime.Today;
        
        // Assert
        Assert.True(now > DateTime.MinValue);
        Assert.True(now <= DateTime.UtcNow.AddSeconds(1));
        Assert.NotEqual(default(DateTime), today);
    }
    
    [Fact]
    public void MathOperations_ShouldWork()
    {
        // Arrange & Act
        int sum = 42 + 58;
        double sqrt = Math.Sqrt(144);
        double power = Math.Pow(2, 10);
        
        // Assert
        Assert.Equal(100, sum);
        Assert.Equal(12.0, sqrt);
        Assert.Equal(1024.0, power);
    }
    
    [Fact]
    public void CollectionOperations_ShouldWork()
    {
        // Arrange
        var numbers = new List<int> { 1, 2, 3, 4, 5 };
        
        // Act
        var doubled = numbers.Select(n => n * 2).ToList();
        var sum = numbers.Sum();
        var filtered = numbers.Where(n => n > 2).ToList();
        
        // Assert
        Assert.Equal(5, numbers.Count);
        Assert.Equal(new List<int> { 2, 4, 6, 8, 10 }, doubled);
        Assert.Equal(15, sum);
        Assert.Equal(3, filtered.Count);
    }
    
    [Fact]
    public async Task AsyncOperations_ShouldWork()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        
        // Act
        await Task.Delay(50);
        var endTime = DateTime.UtcNow;
        
        // Assert
        var elapsed = (endTime - startTime).TotalMilliseconds;
        Assert.True(elapsed >= 40); // Allow some tolerance
    }
    
    [Fact]
    public void LinqOperations_ShouldWork()
    {
        // Arrange
        var data = Enumerable.Range(1, 10).ToList();
        
        // Act
        var evenNumbers = data.Where(x => x % 2 == 0).ToList();
        var squares = data.Select(x => x * x).ToList();
        var sum = data.Sum();
        
        // Assert
        Assert.Equal(5, evenNumbers.Count);
        Assert.Equal(100, squares.Last());
        Assert.Equal(55, sum);
    }
    
    [Fact]
    public void DictionaryOperations_ShouldWork()
    {
        // Arrange
        var dict = new Dictionary<string, int>
        {
            { "one", 1 },
            { "two", 2 },
            { "three", 3 }
        };
        
        // Act & Assert
        Assert.True(dict.ContainsKey("one"));
        Assert.Equal(1, dict["one"]);
        Assert.Equal(3, dict.Count);
        
        dict["four"] = 4;
        Assert.Equal(4, dict.Count);
    }
    
    [Fact]
    public void StringManipulation_ShouldWork()
    {
        // Arrange
        string text = "Hello WASI World";
        
        // Act
        string upper = text.ToUpper();
        string lower = text.ToLower();
        string replaced = text.Replace("WASI", "WebAssembly");
        string[] words = text.Split(' ');
        
        // Assert
        Assert.Equal("HELLO WASI WORLD", upper);
        Assert.Equal("hello wasi world", lower);
        Assert.Equal("Hello WebAssembly World", replaced);
        Assert.Equal(3, words.Length);
    }
    
    [Theory]
    [InlineData(1, 1, 2)]
    [InlineData(5, 5, 10)]
    [InlineData(-1, 1, 0)]
    [InlineData(100, 200, 300)]
    public void Addition_ShouldReturnCorrectSum(int a, int b, int expected)
    {
        // Act
        int result = a + b;
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Fact]
    public void RuntimeInformation_ShouldBeAvailable()
    {
        // Act
        var runtimeIdentifier = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;
        var osDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        
        // Assert
        Assert.NotNull(runtimeIdentifier);
        Assert.NotEmpty(runtimeIdentifier);
        Assert.NotNull(osDescription);
    }
}