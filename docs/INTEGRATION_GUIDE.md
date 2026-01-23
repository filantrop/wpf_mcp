# WPF-MCP Integration Guide

This guide explains how to integrate the WPF-MCP server with Claude and other MCP-compatible clients.

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Claude Desktop Integration](#claude-desktop-integration)
- [Claude Code CLI Integration](#claude-code-cli-integration)
- [Best Practices for AI Agents](#best-practices-for-ai-agents)
- [Troubleshooting](#troubleshooting)

---

## Overview

WPF-MCP uses the Model Context Protocol (MCP) to expose WPF automation tools to AI agents. The server communicates via stdio transport using JSON-RPC 2.0.

**Supported Clients:**
- Claude Desktop (Anthropic)
- Claude Code CLI
- Any MCP-compatible client

**Transport:** stdio (stdin/stdout)

---

## Installation

### Prerequisites

1. **Windows 10/11** - UI Automation only works on Windows
2. **.NET 8.0 SDK** - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
3. **WPF-MCP Server** - Clone and build the repository

### Build Steps

```bash
# Clone the repository
git clone https://github.com/your-org/WPF-mcp.git
cd WPF-mcp

# Restore dependencies
dotnet restore

# Build
dotnet build

# Verify build
dotnet run --project src/WpfMcp.Server --help
```

### Verify Installation

Run the server directly to verify it starts correctly:

```bash
dotnet run --project src/WpfMcp.Server
```

The server will wait for MCP protocol messages on stdin. Press Ctrl+C to exit.

---

## Claude Desktop Integration

### Configuration File Location

The Claude Desktop configuration file is located at:

| OS | Path |
|----|------|
| Windows | `%APPDATA%\Claude\claude_desktop_config.json` |
| macOS | `~/Library/Application Support/Claude/claude_desktop_config.json` |

### Configuration Options

**Option 1: Using dotnet run (Development)**

```json
{
  "mcpServers": {
    "wpf-mcp": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:/Users/YourName/Projects/WPF-mcp/src/WpfMcp.Server"
      ]
    }
  }
}
```

**Option 2: Using Compiled Executable (Production)**

```json
{
  "mcpServers": {
    "wpf-mcp": {
      "command": "C:/Users/YourName/Projects/WPF-mcp/src/WpfMcp.Server/bin/Release/net8.0-windows/win-x64/WpfMcp.Server.exe"
    }
  }
}
```

**Option 3: With Working Directory**

```json
{
  "mcpServers": {
    "wpf-mcp": {
      "command": "WpfMcp.Server.exe",
      "cwd": "C:/Users/YourName/Projects/WPF-mcp/src/WpfMcp.Server/bin/Release/net8.0-windows/win-x64"
    }
  }
}
```

### Restart Claude Desktop

After editing the configuration:
1. Completely close Claude Desktop
2. Reopen Claude Desktop
3. The WPF-MCP tools should now be available

### Verify Connection

Ask Claude: "What WPF tools do you have available?"

Claude should list the wpf_* tools if the server is connected correctly.

---

## Claude Code CLI Integration

### Configuration

Add to your Claude Code settings (`.claude/settings.json` or global settings):

```json
{
  "mcpServers": {
    "wpf-mcp": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/path/to/WPF-mcp/src/WpfMcp.Server"]
    }
  }
}
```

### Using with Claude Code

```bash
# Start Claude Code with MCP servers
claude

# Ask Claude to use WPF tools
> Launch Notepad and type "Hello World"
```

---

## Best Practices for AI Agents

### Workflow Pattern

Follow this general pattern when automating WPF applications:

```
1. LAUNCH/ATTACH → Connect to target application
2. SNAPSHOT → Understand current UI state
3. INTERACT → Perform actions on elements
4. VERIFY → Take new snapshot to confirm changes
5. REPEAT → Continue as needed
```

### Always Snapshot First

Before interacting with any element, take a snapshot to get fresh references:

```
User: "Click the save button"
Agent:
  1. Call wpf_snapshot()
  2. Find the save button in the tree
  3. Call wpf_click(element="Save button", ref="e5")
```

### Handle Stale References

Element references become invalid when:
- A new snapshot is taken
- The UI structure changes
- The application refreshes

If you get `ELEMENT_NOT_FOUND` or `ELEMENT_STALE`, take a new snapshot.

### Use Descriptive Element Parameters

The `element` parameter is for human understanding, not automation:

```json
// Good - descriptive
{
  "element": "Save Changes button in the toolbar",
  "ref": "e12"
}

// Bad - not helpful
{
  "element": "e12",
  "ref": "e12"
}
```

### Wait for UI Stability

After actions that trigger UI changes, wait before proceeding:

```
1. wpf_click(element="Login", ref="e5")
2. wpf_wait_for(condition="exists", timeout_ms=5000)
3. wpf_snapshot()
```

### Handle Errors Gracefully

Each error includes a `suggestion` field with recovery steps:

```json
{
  "success": false,
  "error": {
    "code": "ELEMENT_NOT_VISIBLE",
    "message": "Element 'Submit' is not visible on screen",
    "suggestion": "Call wpf_scroll_into_view to make the element visible first"
  }
}
```

Follow the suggestion to recover from errors.

### Prefer set_value Over type

For text inputs, `wpf_set_value` is faster than `wpf_type`:

```
// Faster - direct value setting
wpf_set_value(element="Email", ref="e3", value="user@example.com")

// Slower but triggers key events
wpf_type(element="Email", ref="e3", text="user@example.com")
```

Use `wpf_type` only when the application needs keystroke events.

---

## Troubleshooting

### Server Won't Start

**Symptom:** Claude says WPF tools are not available

**Solutions:**
1. Verify .NET 8.0 is installed: `dotnet --version`
2. Check the path in configuration is correct
3. Try running the server manually to see errors
4. Check Claude Desktop logs for connection errors

### No Application Attached Error

**Symptom:** `APP_NOT_ATTACHED` errors

**Solution:** Always launch or attach before other operations:

```
wpf_launch_application(path="C:\\MyApp\\App.exe")
```

### Element References Not Found

**Symptom:** `ELEMENT_NOT_FOUND` errors

**Solutions:**
1. Take a fresh snapshot: `wpf_snapshot()`
2. Check if the element is visible (not scrolled out of view)
3. Verify the application UI hasn't changed
4. Increase `max_depth` if element is deeply nested

### Application Not Responding

**Symptom:** `APP_NOT_RESPONDING` or timeout errors

**Solutions:**
1. Check if the target application is frozen
2. Look for modal dialogs blocking the main window
3. Increase timeout values
4. Try force-closing and relaunching: `wpf_close_application(force=true)`

### Permission Denied

**Symptom:** Cannot attach to certain applications

**Solutions:**
1. Run Claude Desktop as Administrator
2. Some applications have UI Automation disabled
3. Check Windows Security settings

### Slow Snapshots

**Symptom:** `wpf_snapshot` takes too long

**Solutions:**
1. Reduce `max_depth` (default is 5)
2. Use `root_ref` to snapshot only a subtree
3. Avoid `include_invisible=true` unless necessary

### Wrong Element Clicked

**Symptom:** Actions happen on unexpected elements

**Solutions:**
1. Use `wpf_get_element_properties` to verify element details
2. Check if multiple elements have the same name
3. Use `automation_id` for more precise targeting
4. Take a screenshot to visually verify the UI state

---

## Example Conversations

### Basic Form Filling

```
User: Fill out the registration form with my info:
      Name: John Doe
      Email: john@example.com
      Accept terms and submit

Agent:
1. Let me take a snapshot of the current UI...
   [calls wpf_snapshot()]

2. I see the registration form. Let me fill it out:
   [calls wpf_type(element="Name field", ref="e3", text="John Doe")]
   [calls wpf_type(element="Email field", ref="e4", text="john@example.com")]
   [calls wpf_toggle(element="Accept terms", ref="e5", target_state="on")]
   [calls wpf_click(element="Submit button", ref="e6")]

3. Waiting for confirmation...
   [calls wpf_wait_for(condition="exists", timeout_ms=5000)]

4. Done! The form has been submitted successfully.
```

### Multi-Window Application

```
User: Open settings and enable dark mode

Agent:
1. Let me check what windows are available...
   [calls wpf_list_windows()]

2. I see the main window. Let me look for a settings option...
   [calls wpf_snapshot()]

3. I found a Settings menu. Opening it...
   [calls wpf_click(element="Settings menu", ref="e8")]

4. A settings dialog opened. Let me switch to it...
   [calls wpf_switch_window(window_title="Settings")]
   [calls wpf_snapshot()]

5. Found the dark mode toggle. Enabling it...
   [calls wpf_toggle(element="Dark mode", ref="e12", target_state="on")]
   [calls wpf_click(element="Save", ref="e15")]

6. Dark mode is now enabled!
```

---

## Security Considerations

### Running as Administrator

Some applications require administrator privileges for UI Automation access. However, running Claude Desktop as admin gives it elevated permissions. Consider the security implications.

### Sensitive Data

- Avoid typing passwords directly; use secure input methods when possible
- Screenshots may capture sensitive information
- Element names and values are visible in tool responses

### Process Isolation

The WPF-MCP server runs as a separate process. It can only interact with applications that allow UI Automation access.

---

## Getting Help

- **Documentation:** See [ARCHITECTURE.md](ARCHITECTURE.md) and [TOOLS_REFERENCE.md](TOOLS_REFERENCE.md)
- **Issues:** Report bugs on the GitHub repository
- **MCP Protocol:** [Model Context Protocol Documentation](https://modelcontextprotocol.io)
