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
echo "Installing wasi-sdk for Release wasm publish..."
TAG_VERSION="29"
WASI_SDK_VERSION="29.0"
WASI_SDK_URL="https://github.com/WebAssembly/wasi-sdk/releases/download/wasi-sdk-${TAG_VERSION}/wasi-sdk-${WASI_SDK_VERSION}-arm64-macos.tar.gz"
WASI_SDK_DIR="/opt/wasi-sdk-${WASI_SDK_VERSION}"
if [ ! -d "$WASI_SDK_DIR" ]; then
  curl -L -o wasi-sdk.tar.gz "$WASI_SDK_URL"
  echo "Downloaded wasi-sdk.tar.gz:"
  ls -l wasi-sdk.tar.gz
  file wasi-sdk.tar.gz
  head -c 20 wasi-sdk.tar.gz | od -c
  sudo mkdir -p /opt
  sudo tar xzf wasi-sdk.tar.gz -C /opt
  rm wasi-sdk.tar.gz
fi
export WASI_SDK_PATH="$WASI_SDK_DIR"
echo "WASI_SDK_PATH=$WASI_SDK_PATH (add to ~/.zshrc for persistence)"

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