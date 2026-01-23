# Agent Build Instructions - WPF-MCP Server

## Project Overview

This is a Model Context Protocol (MCP) server for automating WPF applications. It uses FlaUI and Microsoft UI Automation to enable AI agents to interact with WPF desktop applications.

## Prerequisites

- .NET 8.0 SDK or later
- Windows operating system (required for UI Automation)
- Visual Studio 2022 or Rider (optional, for IDE development)

## Project Setup

```bash
# Navigate to project directory
cd /mnt/c/Users/Riccardo/Documents/Coding/mcp/WPF-mcp

# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/WpfMcp.Server.Tests/WpfMcp.Server.Tests.csproj
```

## Build Commands

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Publish for distribution
dotnet publish src/WpfMcp.Server/WpfMcp.Server.csproj -c Release -r win-x64 --self-contained false
```

## Running the MCP Server

```bash
# Run the server (stdio transport)
dotnet run --project src/WpfMcp.Server/WpfMcp.Server.csproj
```

## MCP Client Configuration

To use with Claude Desktop or other MCP clients, add to your MCP configuration:

```json
{
  "mcpServers": {
    "wpf-mcp": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/path/to/WPF-mcp/src/WpfMcp.Server/WpfMcp.Server.csproj"]
    }
  }
}
```

Or with a published executable:

```json
{
  "mcpServers": {
    "wpf-mcp": {
      "command": "C:/path/to/WPF-mcp/publish/WpfMcp.Server.exe"
    }
  }
}
```

## Key Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| ModelContextProtocol | 0.2.0-preview.1 | MCP server framework |
| FlaUI.Core | 5.0.0 | UI Automation abstraction |
| FlaUI.UIA3 | 5.0.0 | UIA3 implementation |
| Microsoft.Extensions.Hosting | 8.0.0 | Host builder and DI |

## Available MCP Tools

### Application Management
- `wpf_launch_application` - Launch a WPF application
- `wpf_attach_application` - Attach to a running application
- `wpf_close_application` - Close the attached application

### Element Discovery
- `wpf_snapshot` - Get accessibility tree snapshot
- `wpf_find_element` - Find elements by criteria
- `wpf_get_element_properties` - Get element details

### Interaction
- `wpf_click` - Click elements
- `wpf_type` - Type text
- `wpf_set_value` - Set value directly
- `wpf_toggle` - Toggle checkboxes
- `wpf_select` - Select items in lists
- `wpf_expand_collapse` - Expand/collapse tree nodes
- `wpf_press_key` - Send keyboard input

### Navigation
- `wpf_scroll` - Scroll within containers
- `wpf_scroll_into_view` - Scroll element into view
- `wpf_focus` - Set keyboard focus

### Window Management
- `wpf_list_windows` - List application windows
- `wpf_switch_window` - Switch to a window
- `wpf_window_action` - Minimize/maximize/close windows

### Utilities
- `wpf_take_screenshot` - Capture screenshots
- `wpf_wait_for` - Wait for conditions

## Key Learnings

### Element References
- Element refs (e.g., `e1`, `e2`) are only valid within a snapshot context
- Call `wpf_snapshot` to get fresh references after UI changes
- The `element` parameter in tools is for audit/logging only; `ref` is the actual identifier

### Best Practices
- Always call `wpf_snapshot` before interacting with elements
- Use `wpf_wait_for` to wait for elements to be ready
- Prefer `wpf_set_value` over `wpf_type` for faster input when ValuePattern is supported
- Check `wpf_get_element_properties` to see supported patterns

### Known Limitations
- Only works on Windows (UI Automation is Windows-specific)
- Requires application to have proper AutomationIds for reliable automation
- Some custom controls may not expose proper automation peers

## Feature Development Quality Standards

**CRITICAL**: All new features MUST meet the following mandatory requirements before being considered complete.

### Testing Requirements

- **Minimum Coverage**: 85% code coverage ratio required for all new code
- **Test Pass Rate**: 100% - all tests must pass, no exceptions
- **Test Types Required**:
  - Unit tests for all business logic and services
  - Integration tests for API endpoints or main functionality
  - End-to-end tests for critical user workflows

### Git Workflow Requirements

Before moving to the next feature, ALL changes must be:

1. **Committed with Clear Messages**:
   ```bash
   git add .
   git commit -m "feat(module): descriptive message following conventional commits"
   ```
   - Use conventional commit format: `feat:`, `fix:`, `docs:`, `test:`, `refactor:`, etc.

2. **Pushed to Remote Repository** (when configured)

### Feature Completion Checklist

Before marking ANY feature as complete, verify:

- [ ] All tests pass with `dotnet test`
- [ ] Code compiles without errors with `dotnet build`
- [ ] All changes committed with conventional commit messages
- [ ] .ralph/@fix_plan.md task marked as complete
- [ ] .ralph/@AGENT.md updated (if new patterns introduced)
