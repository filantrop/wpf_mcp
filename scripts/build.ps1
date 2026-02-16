# Build the project
# Usage: .\scripts\build.ps1 [-Release]

param(
    [switch]$Release
)

$config = if ($Release) { "Release" } else { "Debug" }

Write-Host "Building WPF-MCP Server ($config)..." -ForegroundColor Cyan

dotnet build src/WpfMcp.Server -c $config

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
} else {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
