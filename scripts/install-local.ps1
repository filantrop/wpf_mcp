# Install the tool locally for testing
# Usage: .\scripts\install-local.ps1

Write-Host "Installing WPF-MCP as global tool..." -ForegroundColor Cyan

# Uninstall if already installed
dotnet tool uninstall --global WpfMcp.Server 2>$null

# Pack first
& "$PSScriptRoot\pack.ps1"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Pack failed, cannot install" -ForegroundColor Red
    exit 1
}

# Install from local package
dotnet tool install --global --add-source ./nupkg WpfMcp.Server

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Installation successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now use 'wpf-mcp' command globally." -ForegroundColor Gray
    Write-Host ""
    Write-Host "Add to your MCP client config:" -ForegroundColor Gray
    Write-Host '  {' -ForegroundColor White
    Write-Host '    "mcpServers": {' -ForegroundColor White
    Write-Host '      "wpf-mcp": {' -ForegroundColor White
    Write-Host '        "command": "wpf-mcp",' -ForegroundColor White
    Write-Host '        "args": []' -ForegroundColor White
    Write-Host '      }' -ForegroundColor White
    Write-Host '    }' -ForegroundColor White
    Write-Host '  }' -ForegroundColor White
} else {
    Write-Host "Installation failed!" -ForegroundColor Red
    exit 1
}
