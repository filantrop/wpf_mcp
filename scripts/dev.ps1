# Development mode with auto-rebuild on file changes
# Usage: .\scripts\dev.ps1

Write-Host "Starting WPF-MCP Server in development mode..." -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop" -ForegroundColor Gray

dotnet watch run --project src/WpfMcp.Server
