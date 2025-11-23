#!/bin/bash

# Setup script for wadoocore .NET 10 WASI project on macOS/Linux
# Run with: chmod +x setup.sh && ./setup.sh

set -euo pipefail

echo "=== Setting up wadoocore (.NET 10 WASI) ==="

# Verify .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK not found in PATH."
    echo "Please install .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0"
    echo "On macOS: brew install --cask dotnet-sdk"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo ".NET SDK version: $DOTNET_VERSION"

# Update workloads and install wasi-experimental
echo "Updating workloads..."
dotnet workload update
echo "Installing wasi-experimental workload..."
dotnet workload install wasi-experimental

# Restore dependencies
echo "Restoring NuGet packages..."
dotnet restore

# Build the solution
echo "Building the project..."
dotnet build --no-restore

# Run the Hello World tester
echo "Running Hello World tester on WASI..."
dotnet run --project src/wadoocore --no-build

echo "=== Setup complete! ==="
echo "You can now:"
echo "- Open wadoocore.sln in VSCode"
echo "- Use Ctrl+Shift+P > Tasks: Run Task > run"
echo "- Debug with F5"