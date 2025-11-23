#!/bin/bash
# Script to run the wadoocore WASI application

set -e

echo "=== Building wadoocore for WASI ==="
dotnet build src/wadoocore/wadoocore.csproj -c Debug

echo ""
echo "=== Preparing WASI environment ==="
# Copy all dependencies to AppBundle for easier execution
cp src/wadoocore/bin/Debug/net10.0/wasi-wasm/* src/wadoocore/bin/Debug/net10.0/wasi-wasm/AppBundle/ 2>/dev/null || :

echo "=== Running wadoocore on WASI ==="
cd src/wadoocore/bin/Debug/net10.0/wasi-wasm/AppBundle

# Run with wasmtime enabling HTTP support
# We map the current directory to / to simplify file access
wasmtime run \
  -S http \
  --dir .::/ \
  --env PWD=/ \
  dotnet.wasm \
  wadoocore

cd ../../../../../..