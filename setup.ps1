# Setup script for wadoocore .NET 10 WASI project on Windows
# Run with: powershell -ExecutionPolicy Bypass -File .\setup.ps1
# or in PowerShell: .\setup.ps1

Write-Host "=== Setting up wadoocore (.NET 10 WASI) ===" -ForegroundColor Green

# Verify .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Host ".NET SDK version: $dotnetVersion" -ForegroundColor Yellow
} catch {
    Write-Error ".NET SDK not found in PATH."
    Write-Host "Please install .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Red
    Write-Host "Restart PowerShell after installation." -ForegroundColor Red
    exit 1
}

# Update workloads and install wasi-experimental
Write-Host "Updating workloads..." -ForegroundColor Yellow
dotnet workload update

Write-Host "Installing wasi-experimental workload (may take a few minutes)..." -ForegroundColor Yellow
dotnet workload install wasi-experimental

# Restore dependencies
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore

# Build the solution
Write-Host "Building the project..." -ForegroundColor Yellow
dotnet build --no-restore

# Run the Hello World tester
Write-Host "Running Hello World tester on WASI..." -ForegroundColor Yellow
dotnet run --project src/wadoocore --no-build

Write-Host "=== Setup complete! ===" -ForegroundColor Green
Write-Host "You can now:" -ForegroundColor Cyan
Write-Host "- Open wadoocore.sln in VSCode" -ForegroundColor Cyan
Write-Host "- Use Ctrl+Shift+P > Tasks: Run Task > run" -ForegroundColor Cyan
Write-Host "- Debug with F5" -ForegroundColor Cyan