# Wadoocore

A best-practice .NET 10 project targeting WASI (WebAssembly System Interface) for VSCode.

This project includes a simple "Hello World" console application that runs on WASI.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- VSCode with C# extension

## Quick Setup for New Developers

1. Run the setup script for your platform:
   - macOS/Linux: `./setup.sh`
   - Windows: `.\setup.ps1`

The setup script will:
- Install the `wasi-experimental` workload if missing
- Restore dependencies
- Build the project
- Run the Hello World tester

## Project Structure

```
wadoocore/
├── global.json          # Pins .NET SDK version
├── wadoocore.sln        # Solution file
├── src/
│   └── wadoocore/       # Main project
│       ├── wadoocore.csproj
│       └── Program.cs   # Hello World tester
├── .gitignore
├── README.md
├── setup.sh
├── setup.ps1
└── .vscode/             # VSCode tasks and launch configs
    ├── tasks.json
    └── launch.json
```

## Build and Run

```bash
dotnet restore
dotnet build
dotnet run --project src/wadoocore
```

Expected output:
```
Hello World from .NET on WASI!
```

## Publish for WASI

```bash
dotnet publish src/wadoocore/wadoocore.csproj -c Release -o publish
```

This generates a WebAssembly executable (`.wasm`) runnable with tools like `wasmtime`.

## Tasks in VSCode

Use Ctrl+Shift+P > Tasks: Run Task for:
- Restore
- Build
- Run
- Publish

## Debugging

Use F5 or launch.json configurations for debugging the .NET app (WASI debugging experimental).

## WASI Notes

- Targets `net10.0-wasi-experimental`
- Console output goes to WASI stdout
- Limited APIs compared to full .NET