# WPF-MCP Architecture Guide

This document describes the technical architecture and design decisions of the WPF-MCP Server.

## Overview

WPF-MCP is a Model Context Protocol (MCP) server that bridges AI agents with WPF desktop applications through Windows UI Automation.

```
┌─────────────────────────────┐
│   AI Agent (Claude)         │  Issues MCP tool calls
├─────────────────────────────┤
│   MCP Protocol Layer        │  Tool registration & JSON-RPC 2.0
│   (ModelContextProtocol)    │  stdio transport
├─────────────────────────────┤
│   WPF Automation Layer      │  Element tree building
│   (Tool & Service Classes)  │  Pattern execution
├─────────────────────────────┤
│   FlaUI.UIA3 Layer          │  COM-based UI Automation
│   (Windows UI Automation)   │  Element discovery
├─────────────────────────────┤
│   WPF Application           │  Automation peers
│                             │  UI Automation patterns
└─────────────────────────────┘
```

## Technology Stack

| Component | Technology | Version | Purpose |
|-----------|------------|---------|---------|
| Runtime | .NET | 8.0 (LTS) | Windows-specific features |
| MCP SDK | ModelContextProtocol | 0.6.0-preview.1 | Official MCP framework |
| UI Automation | FlaUI.Core + FlaUI.UIA3 | 5.0.0 | WPF automation abstraction |
| Hosting | Microsoft.Extensions.Hosting | 8.0.0 | DI and service lifecycle |
| Transport | stdio | - | JSON-RPC 2.0 over stdin/stdout |
| Testing | xUnit + FluentAssertions | 2.7.0 / 6.12.0 | Unit testing |

## Project Structure

```
src/WpfMcp.Server/
├── Program.cs                    # Entry point & DI configuration
├── WpfMcp.Server.csproj
│
├── Models/                       # Data transfer objects
│   ├── ToolResponse.cs           # Generic response wrapper
│   ├── ErrorCodes.cs             # Error code constants
│   ├── SnapshotElement.cs        # Accessibility tree node
│   └── ElementReference.cs       # Element metadata
│
├── Services/                     # Core business logic
│   ├── IApplicationManager.cs    # App lifecycle interface
│   ├── ApplicationManager.cs     # Implementation
│   ├── IElementReferenceManager.cs
│   └── ElementReferenceManager.cs
│
└── Tools/                        # MCP tool implementations
    ├── WpfApplicationTools.cs    # Launch/attach/close
    ├── WpfSnapshotTools.cs       # Element discovery
    ├── WpfInteractionTools.cs    # Click/type/toggle
    ├── WpfNavigationTools.cs     # Scroll/focus
    ├── WpfWindowTools.cs         # Window management
    └── WpfUtilityTools.cs        # Screenshot/wait
```

## Core Components

### 1. ApplicationManager

Manages the lifecycle of the target WPF application.

```csharp
public interface IApplicationManager
{
    bool IsAttached { get; }
    Window? MainWindow { get; }

    Task<Window> LaunchApplicationAsync(string path, string[]? args, int timeoutMs);
    Task<Window> AttachByNameAsync(string processName);
    Task<Window> AttachByIdAsync(int processId);
    Task CloseApplicationAsync(bool force);

    IReadOnlyList<Window> GetAllWindows();
    bool HasApplicationCrashed();
}
```

**Responsibilities:**
- Start new processes and wait for main window
- Attach to existing processes
- Track process health (crash detection)
- Manage window collection

**Design Decision:** Single application mode. Only one application can be attached at a time, simplifying state management and preventing cross-app reference confusion.

### 2. ElementReferenceManager

Manages element references within snapshot contexts.

```csharp
public interface IElementReferenceManager
{
    int ElementCount { get; }

    void BeginNewSnapshot();
    ElementReference RegisterElement(AutomationElement element);
    AutomationElement? GetElement(string refId);
    ElementReference? GetReference(string refId);
    bool IsReferenceValid(string refId);
    void Clear();
}
```

**Reference Format:** `e1`, `e2`, `e3`, etc. (incrementing per snapshot)

**Responsibilities:**
- Assign short reference IDs to elements
- Store element metadata (type, name, bounds)
- Validate reference freshness
- Manage snapshot boundaries

**Design Decision:** Snapshot-scoped references. Each `wpf_snapshot` call starts a new context, invalidating all previous references. This prevents stale reference bugs and forces explicit refresh.

### 3. Tool Classes

Tools are organized by domain, each decorated with `[McpServerToolType]`:

| Class | Domain | Tool Count |
|-------|--------|------------|
| WpfApplicationTools | App lifecycle | 3 |
| WpfSnapshotTools | Element discovery | 3 |
| WpfInteractionTools | UI manipulation | 7 |
| WpfNavigationTools | Scroll & focus | 3 |
| WpfWindowTools | Window management | 3 |
| WpfUtilityTools | Screenshots, waits | 2 |

**Tool Registration:**
```csharp
[McpServerToolType]
public sealed class WpfInteractionTools
{
    [McpServerTool(Name = "wpf_click"), Description("Clicks an element")]
    public string Click(
        [Description("Human-readable description")] string element,
        [Description("Element reference from snapshot")] string @ref,
        [Description("Click type: single, double, right")] string click_type = "single")
    {
        // Implementation
    }
}
```

## Data Models

### ToolResponse<T>

All tools return a standardized response:

```json
{
  "success": true,
  "data": { /* tool-specific payload */ },
  "error": null,
  "metadata": {
    "execution_time_ms": 42,
    "warnings": [],
    "snapshot_valid": true
  }
}
```

Error response:
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "ELEMENT_NOT_FOUND",
    "message": "Element with ref 'e5' not found",
    "suggestion": "Call wpf_snapshot to refresh references",
    "recoverable": true
  },
  "metadata": { ... }
}
```

### SnapshotElement

Represents a node in the accessibility tree:

```csharp
public class SnapshotElement
{
    public string Ref { get; set; }           // "e1"
    public string ControlType { get; set; }   // "Button"
    public string? Name { get; set; }         // "Save"
    public string? AutomationId { get; set; } // "btnSave"
    public string? Value { get; set; }        // Current value
    public List<string> States { get; set; }  // ["enabled", "focused"]
    public int Depth { get; set; }
    public List<SnapshotElement> Children { get; set; }
}
```

**YAML Serialization:**
```yaml
- window "Main Window" [ref=e1]
  - button "Save" [ref=e2] [enabled]
  - textbox "Username" [ref=e3] [value="john"] [focused]
  - checkbox "Remember" [ref=e4] [unchecked]
```

### ErrorCodes

Predefined error constants for consistent error handling:

| Code | Description |
|------|-------------|
| `APP_NOT_ATTACHED` | No application connected |
| `APP_CRASHED` | Process has exited |
| `APP_NOT_RESPONDING` | UI thread blocked |
| `ELEMENT_NOT_FOUND` | Reference doesn't exist |
| `ELEMENT_STALE` | Reference outdated |
| `ELEMENT_NOT_ENABLED` | Element is disabled |
| `ELEMENT_NOT_VISIBLE` | Element is off-screen |
| `ELEMENT_READ_ONLY` | Cannot modify element |
| `PATTERN_NOT_SUPPORTED` | Automation pattern unavailable |
| `TIMEOUT` | Operation exceeded timeout |
| `VALUE_TOO_LONG` | Text exceeds 10KB limit |
| `ITEM_NOT_FOUND` | Dropdown item not found |
| `WINDOW_NOT_FOUND` | Window doesn't exist |
| `FILE_NOT_FOUND` | Executable path invalid |
| `LAUNCH_FAILED` | Failed to start process |
| `INVALID_PARAMETER` | Bad input parameter |

## Design Patterns

### 1. Dual Parameter Design

Interaction tools accept both human description and machine reference:

```csharp
wpf_click(
    element: "Save button",  // For audit/logging
    ref: "e5"                // For execution
)
```

The `element` parameter is never validated - it's purely for logging and permission prompts. The `ref` is the authoritative identifier.

### 2. Lazy Pattern Validation

Tools check pattern support before use, with graceful fallbacks:

```csharp
if (element.Patterns.Invoke.IsSupported)
{
    element.Patterns.Invoke.Pattern.Invoke();
}
else
{
    // Fallback to mouse click
    Mouse.Click(element.GetClickablePoint());
}
```

### 3. No Automatic Retries

The server deliberately does NOT auto-retry failed operations:

- State changes between failure and retry are unpredictable
- AI agents should make explicit retry decisions
- Each error includes actionable recovery suggestions

### 4. State Tracking

Elements report multiple states automatically:

| State | Detection Method |
|-------|------------------|
| `disabled` | `!IsEnabled` |
| `focused` | `HasKeyboardFocus` |
| `checked/unchecked` | TogglePattern state |
| `selected` | SelectionItemPattern |
| `expanded/collapsed` | ExpandCollapsePattern |
| `readonly` | ValuePattern.IsReadOnly |
| `modal` | WindowPattern.IsModal |

## Entry Point

`Program.cs` configures the MCP server:

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Register services
builder.Services.AddSingleton<IApplicationManager, ApplicationManager>();
builder.Services.AddSingleton<IElementReferenceManager, ElementReferenceManager>();

// Register tool classes
builder.Services.AddSingleton<WpfApplicationTools>();
builder.Services.AddSingleton<WpfSnapshotTools>();
// ... other tools

// Configure MCP
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new() { Name = "wpf-mcp", Version = "1.0.0" };
})
.WithStdioServerTransport()
.WithToolsFromAssembly(typeof(Program).Assembly);

await builder.Build().RunAsync();
```

## Performance Targets

| Operation | Elements | P95 Target | P99 Target |
|-----------|----------|------------|------------|
| `wpf_snapshot` | ≤100 | 200ms | 500ms |
| `wpf_snapshot` | 100-500 | 500ms | 1000ms |
| `wpf_snapshot` | 500-1000 | 1000ms | 2000ms |
| `wpf_click` (Invoke) | - | 100ms | 200ms |
| `wpf_click` (mouse) | - | 300ms | 500ms |
| `wpf_type` | ≤100 chars | 500ms | 1000ms |
| `wpf_set_value` | - | 100ms | 200ms |

## Security Considerations

1. **No Remote Access** - stdio transport only, local execution
2. **Permission Model** - Human descriptions shown in permission prompts
3. **No Credential Storage** - No passwords or secrets stored
4. **Process Isolation** - Each attached app is separate process
5. **Input Validation** - All parameters validated before use

## Testing Strategy

```
tests/WpfMcp.Server.Tests/
├── Models/
│   ├── ToolResponseTests.cs      # Response serialization
│   └── SnapshotElementTests.cs   # YAML generation
└── Services/
    └── ElementReferenceManagerTests.cs  # Reference lifecycle
```

**Test Categories:**
- Unit tests: Model serialization, service logic
- Integration tests: Full tool execution (requires sample WPF app)

## Future Enhancements

1. **Additional Frameworks** - WinForms, UWP, WinUI 3 support
2. **Recording Mode** - Capture user interactions as tool sequences
3. **Visual Targeting** - Click by image/coordinate
4. **Remote Sessions** - SSE transport for remote automation
5. **Multi-App Support** - Manage multiple applications simultaneously
