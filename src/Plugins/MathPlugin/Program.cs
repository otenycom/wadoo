using System.Runtime.InteropServices;

// This MathPlugin is compiled to WASM and exports mathematical functions
// that can be called from the host using wasmtime.net

public class MathPlugin
{
    // Core implementation methods (can be called from C#)
    public static int AddImpl(int a, int b) => a + b;
    public static int SubtractImpl(int a, int b) => a - b;
    public static int MultiplyImpl(int a, int b) => a * b;
    public static int DivideImpl(int a, int b) => b == 0 ? 0 : a / b;

    // Exported functions for WASM (UnmanagedCallersOnly)
    [UnmanagedCallersOnly(EntryPoint = "add")]
    public static int Add(int a, int b) => AddImpl(a, b);

    [UnmanagedCallersOnly(EntryPoint = "subtract")]
    public static int Subtract(int a, int b) => SubtractImpl(a, b);

    [UnmanagedCallersOnly(EntryPoint = "multiply")]
    public static int Multiply(int a, int b) => MultiplyImpl(a, b);

    [UnmanagedCallersOnly(EntryPoint = "divide")]
    public static int Divide(int a, int b) => DivideImpl(a, b);
}

// Main entry point for WASI
class Program
{
    static void Main(string[] args)
    {
        // When run as a WASI application, we can test the functions
        if (args.Length >= 3)
        {
            var operation = args[0].ToLowerInvariant();
            var a = int.Parse(args[1]);
            var b = int.Parse(args[2]);

            int result = operation switch
            {
                "add" => MathPlugin.AddImpl(a, b),
                "subtract" => MathPlugin.SubtractImpl(a, b),
                "multiply" => MathPlugin.MultiplyImpl(a, b),
                "divide" => MathPlugin.DivideImpl(a, b),
                _ => throw new ArgumentException($"Unknown operation: {operation}")
            };

            Console.WriteLine(result);
        }
        else
        {
            Console.WriteLine("MathPlugin WASM module");
            Console.WriteLine("Usage: MathPlugin <operation> <a> <b>");
            Console.WriteLine("Operations: add, subtract, multiply, divide");
            Console.WriteLine("Example: MathPlugin add 5 3");
        }
    }
}