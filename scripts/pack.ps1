# Create NuGet package for distribution
# Usage: .\scripts\pack.ps1

Write-Host "Creating NuGet package..." -ForegroundColor Cyan

# Clean previous packages
if (Test-Path "./nupkg") {
    Remove-Item -Recurse -Force "./nupkg"
}

# Create package
dotnet pack src/WpfMcp.Server -c Release -o ./nupkg

if ($LASTEXITCODE -eq 0) {
    Write-Host "Package created successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Package location:" -ForegroundColor Gray
    Get-ChildItem ./nupkg/*.nupkg | ForEach-Object { Write-Host "  $_" -ForegroundColor White }
    Write-Host ""
    Write-Host "To install locally:" -ForegroundColor Gray
    Write-Host "  dotnet tool install --global --add-source ./nupkg WpfMcp.Server" -ForegroundColor White
} else {
    Write-Host "Pack failed!" -ForegroundColor Red
    exit 1
}
