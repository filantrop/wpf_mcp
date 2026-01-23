# WPF-MCP Server Technical Specifications

## 1. Executive Summary

### 1.1 Vision
Create a Model Context Protocol (MCP) server that enables AI agents (Claude, GPT, etc.) to programmatically interact with Windows Presentation Foundation (WPF) applications. This server is analogous to Playwright MCP for web browsers, but specifically designed for WPF desktop applications.

### 1.2 Problem Statement
AI agents currently lack standardized tools to:
- Navigate and interact with WPF desktop applications
- Inspect UI element hierarchies and states
- Perform automated testing and validation of WPF interfaces
- Build AI-powered assistants that can control desktop applications

### 1.3 Solution
A .NET-based MCP server that:
- Exposes WPF automation capabilities through standardized MCP tools
- Uses Microsoft UI Automation (UIA) via FlaUI for reliable element interaction
- Provides accessibility tree snapshots similar to Playwright MCP's approach
- Enables AI agents to discover, inspect, and interact with WPF controls

### 1.4 Target Users
- AI developers building agents that need desktop automation
- QA engineers using AI for WPF application testing
- Developers creating AI-powered desktop assistants
- Enterprises automating WPF-based business applications

---

## 2. Goals and Objectives

### 2.1 Primary Goals
| Goal | Success Criteria |
|------|------------------|
| **AI-First Design** | LLMs can effectively discover and interact with WPF controls without vision models |
| **Reliability** | 99%+ success rate for standard UI operations on automation-ready WPF apps |
| **Developer Experience** | < 5 minutes from .NET SDK installed to first successful WPF interaction |
| **Compatibility** | Works with .NET Framework 4.7.2+ and .NET 6/7/8 WPF applications |

### 2.2 Definition: Automation-Ready WPF Application

An **automation-ready** WPF application meets these criteria:

| Criterion | Requirement |
|-----------|-------------|
| **AutomationId** | All interactive elements (buttons, inputs, lists) have `AutomationProperties.AutomationId` set |
| **Accessibility Names** | Key elements have meaningful `AutomationProperties.Name` or visible labels |
| **Standard Controls** | Uses standard WPF controls or custom controls with proper `AutomationPeer` implementations |
| **No Overlays** | No full-window overlays that block automation tree access |
| **Responsive** | UI thread remains responsive (no blocking operations > 5s) |

### 2.3 Non-Goals (Out of Scope v1.0)
- WinForms automation (future version)
- UWP/WinUI automation (future version)
- Cross-platform support (Windows only)
- Visual/screenshot-based element targeting (accessibility-first approach)
- Recording and playback functionality

---

## 3. Technical Architecture

### 3.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    AI Agent (Claude Code)                   │
│                 Uses MCP tools to interact                  │
└─────────────────────────────┬───────────────────────────────┘
                              │ JSON-RPC 2.0 (stdio/SSE)
┌─────────────────────────────▼───────────────────────────────┐
│                    WPF-MCP Server (.NET 8)                  │
│  ┌────────────────────────────────────────────────────────┐ │
│  │                   MCP Protocol Layer                   │ │
│  │    - Tool registration and invocation                  │ │
│  │    - JSON-RPC message handling                         │ │
│  │    - Capability negotiation                            │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │                WPF Automation Layer                    │ │
│  │    - Element discovery and caching                     │ │
│  │    - Accessibility tree serialization                  │ │
│  │    - Pattern execution (Invoke, Value, Toggle, etc.)   │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │                   FlaUI.UIA3 Layer                     │ │
│  │    - COM-based UI Automation interface                 │ │
│  │    - Element reference management                      │ │
│  │    - Event subscription                                │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────┬───────────────────────────────┘
                              │ UI Automation API
┌─────────────────────────────▼───────────────────────────────┐
│                    WPF Application                          │
│              (AutomationPeers + Patterns)                   │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 Technology Stack

| Component | Technology | Rationale |
|-----------|------------|-----------|
| **Runtime** | .NET 8.0 | LTS, cross-platform tooling, best SDK support |
| **MCP SDK** | ModelContextProtocol (official) | Microsoft/Anthropic maintained |
| **UI Automation** | FlaUI.UIA3 | Modern, actively maintained, best WPF support |
| **Transport** | stdio (primary), HTTP/SSE (optional) | stdio for local, SSE for remote |
| **Serialization** | System.Text.Json | High performance, native support |

### 3.3 Element Reference System

Following Playwright MCP's design, elements will be identified using stable references:

```
Element Reference Format: [ref=e{n}]
- Assigned during tree snapshot
- Valid within current snapshot context
- Reset on new snapshot request
- Includes semantic information for LLM understanding
```

### 3.4 Standard Response Schema

All tools return responses following this unified schema:

```json
{
  "success": true,
  "data": {
    // Tool-specific response data
  },
  "metadata": {
    "execution_time_ms": 45,
    "warnings": [],
    "snapshot_valid": true
  }
}
```

**Error Response Schema**:
```json
{
  "success": false,
  "error": {
    "code": "ELEMENT_NOT_FOUND",
    "message": "Element with ref 'e15' not found in current snapshot",
    "suggestion": "Call wpf_snapshot to refresh element references",
    "recoverable": true
  },
  "metadata": {
    "execution_time_ms": 12
  }
}
```

### 3.5 Dual Parameter Design (element + ref)

Many interaction tools require both `element` (human-readable description) and `ref` (element reference):

| Parameter | Purpose | Used For |
|-----------|---------|----------|
| `element` | Human-readable description | Logging, permissions, audit trail |
| `ref` | Exact element identifier | Actual execution |

**Important**: The `element` description is NOT validated against the actual element. It exists for auditability and permission systems. The `ref` parameter is the authoritative identifier used for execution.

---

## 4. MCP Tools Specification

### 4.1 Application Management Tools

#### `wpf_launch_application`
**Description**: Launch a WPF application and prepare it for automation.

```json
{
  "name": "wpf_launch_application",
  "description": "Launches a WPF application executable and waits for main window",
  "inputSchema": {
    "type": "object",
    "properties": {
      "path": {
        "type": "string",
        "description": "Full path to the WPF application executable"
      },
      "arguments": {
        "type": "array",
        "items": { "type": "string" },
        "description": "Command line arguments for the application"
      },
      "timeout_ms": {
        "type": "integer",
        "default": 30000,
        "minimum": 1000,
        "maximum": 120000,
        "description": "Maximum time to wait for main window to appear (1-120 seconds)"
      }
    },
    "required": ["path"]
  }
}
```

#### `wpf_attach_application`
**Description**: Attach to an already running WPF application.

```json
{
  "name": "wpf_attach_application",
  "description": "Attaches to a running WPF application by process name or ID",
  "inputSchema": {
    "type": "object",
    "properties": {
      "process_name": {
        "type": "string",
        "description": "Name of the process (without .exe)"
      },
      "process_id": {
        "type": "integer",
        "description": "Process ID to attach to"
      }
    }
  }
}
```

#### `wpf_close_application`
**Description**: Close the attached application gracefully.

```json
{
  "name": "wpf_close_application",
  "description": "Closes the currently attached WPF application",
  "inputSchema": {
    "type": "object",
    "properties": {
      "force": {
        "type": "boolean",
        "default": false,
        "description": "Force kill if graceful close fails"
      }
    }
  }
}
```

### 4.2 Element Discovery Tools

#### `wpf_snapshot` (Primary Observation Tool)
**Description**: Get accessibility tree snapshot of the current window/element.

```json
{
  "name": "wpf_snapshot",
  "description": "Returns structured accessibility tree snapshot for LLM analysis",
  "inputSchema": {
    "type": "object",
    "properties": {
      "root_ref": {
        "type": "string",
        "description": "Element reference to use as root (default: main window)"
      },
      "max_depth": {
        "type": "integer",
        "default": 5,
        "minimum": 1,
        "maximum": 20,
        "description": "Maximum tree depth to traverse (1-20)"
      },
      "include_invisible": {
        "type": "boolean",
        "default": false,
        "description": "Include invisible elements in snapshot"
      }
    }
  }
}
```

**Output Format** (YAML-style for LLM readability):
```yaml
- window "Main Window" [ref=e1]
  - menu "File" [ref=e2]
    - menuitem "Open" [ref=e3]
    - menuitem "Save" [ref=e4]
  - grid "DataGrid" [ref=e5]
    - row "Row 1" [ref=e6]
      - cell "John Doe" [ref=e7]
  - button "Submit" [ref=e8] [enabled]
  - textbox "Username" [ref=e9] [value=""] [focused]
  - checkbox "Remember Me" [ref=e10] [unchecked]
  - combobox "Country" [ref=e11] [collapsed] [value="USA"]
```

#### `wpf_find_element`
**Description**: Find specific element by various criteria.

```json
{
  "name": "wpf_find_element",
  "description": "Find element by AutomationId, Name, or ControlType",
  "inputSchema": {
    "type": "object",
    "properties": {
      "automation_id": {
        "type": "string",
        "description": "Unique AutomationId property"
      },
      "name": {
        "type": "string",
        "description": "Element Name property (visible text)"
      },
      "control_type": {
        "type": "string",
        "enum": ["Button", "TextBox", "CheckBox", "ComboBox", "ListBox", "DataGrid", "TreeView", "Menu", "MenuItem", "Tab", "TabItem", "Window", "Custom"],
        "description": "UI Automation control type"
      },
      "root_ref": {
        "type": "string",
        "description": "Search within this element (default: main window)"
      }
    }
  }
}
```

#### `wpf_get_element_properties`
**Description**: Get detailed properties of a specific element.

```json
{
  "name": "wpf_get_element_properties",
  "description": "Returns all automation properties for an element",
  "inputSchema": {
    "type": "object",
    "properties": {
      "ref": {
        "type": "string",
        "description": "Element reference from snapshot"
      }
    },
    "required": ["ref"]
  }
}
```

### 4.3 Interaction Tools

#### `wpf_click`
**Description**: Click on an element.

```json
{
  "name": "wpf_click",
  "description": "Clicks an element using InvokePattern or mouse simulation",
  "inputSchema": {
    "type": "object",
    "properties": {
      "element": {
        "type": "string",
        "description": "Human-readable element description for permission"
      },
      "ref": {
        "type": "string",
        "description": "Element reference from snapshot"
      },
      "click_type": {
        "type": "string",
        "enum": ["single", "double", "right"],
        "default": "single",
        "description": "Type of click to perform"
      }
    },
    "required": ["element", "ref"]
  }
}
```

#### `wpf_type`
**Description**: Enter text into an element.

```json
{
  "name": "wpf_type",
  "description": "Types text into a text input element",
  "inputSchema": {
    "type": "object",
    "properties": {
      "element": {
        "type": "string",
        "description": "Human-readable element description"
      },
      "ref": {
        "type": "string",
        "description": "Element reference from snapshot"
      },
      "text": {
        "type": "string",
        "description": "Text to type"
      },
      "clear_first": {
        "type": "boolean",
        "default": true,
        "description": "Clear existing text before typing"
      }
    },
    "required": ["element", "ref", "text"]
  }
}
```

#### `wpf_set_value`
**Description**: Set value directly (faster than typing for ValuePattern elements).

```json
{
  "name": "wpf_set_value",
  "description": "Sets value directly using ValuePattern",
  "inputSchema": {
    "type": "object",
    "properties": {
      "element": {
        "type": "string",
        "description": "Human-readable element description"
      },
      "ref": {
        "type": "string",
        "description": "Element reference from snapshot"
      },
      "value": {
        "type": "string",
        "description": "Value to set"
      }
    },
    "required": ["element", "ref", "value"]
  }
}
```

#### `wpf_toggle`
**Description**: Toggle checkbox or toggleable element.

```json
{
  "name": "wpf_toggle",
  "description": "Toggles a checkbox or toggle button",
  "inputSchema": {
    "type": "object",
    "properties": {
      "element": {
        "type": "string",
        "description": "Human-readable element description"
      },
      "ref": {
        "type": "string",
        "description": "Element reference from snapshot"
      },
      "target_state": {
        "type": "string",
        "enum": ["on", "off", "toggle"],
        "default": "toggle",
        "description": "Desired state or toggle action"
      }
    },
    "required": ["element", "ref"]
  }
}
```

#### `wpf_select`
**Description**: Select item in ComboBox, ListBox, or similar.

```json
{
  "name": "wpf_select",
  "description": "Selects an item in a selection control",
  "inputSchema": {
    "type": "object",
    "properties": {
      "element": {
        "type": "string",
        "description": "Human-readable element description"
      },
      "ref": {
        "type": "string",
        "description": "Element reference for the container"
      },
      "item": {
        "type": "string",
        "description": "Item text or reference to select"
      },
      "item_ref": {
        "type": "string",
        "description": "Direct reference to item element"
      }
    },
    "required": ["element", "ref"]
  }
}
```

#### `wpf_expand_collapse`
**Description**: Expand or collapse TreeView nodes, expanders, etc.

```json
{
  "name": "wpf_expand_collapse",
  "description": "Expands or collapses an element",
  "inputSchema": {
    "type": "object",
    "properties": {
      "element": {
        "type": "string",
        "description": "Human-readable element description"
      },
      "ref": {
        "type": "string",
        "description": "Element reference from snapshot"
      },
      "action": {
        "type": "string",
        "enum": ["expand", "collapse", "toggle"],
        "default": "toggle",
        "description": "Action to perform"
      }
    },
    "required": ["element", "ref"]
  }
}
```

#### `wpf_press_key`
**Description**: Send keyboard input.

```json
{
  "name": "wpf_press_key",
  "description": "Sends keyboard key press to focused element",
  "inputSchema": {
    "type": "object",
    "properties": {
      "key": {
        "type": "string",
        "description": "Key to press (e.g., 'Enter', 'Tab', 'Escape', 'F1', 'Ctrl+S')"
      },
      "ref": {
        "type": "string",
        "description": "Optional element reference to focus first"
      }
    },
    "required": ["key"]
  }
}
```

### 4.4 Navigation Tools

#### `wpf_scroll`
**Description**: Scroll within a scrollable container.

```json
{
  "name": "wpf_scroll",
  "description": "Scrolls within a scrollable element",
  "inputSchema": {
    "type": "object",
    "properties": {
      "element": {
        "type": "string",
        "description": "Human-readable element description"
      },
      "ref": {
        "type": "string",
        "description": "Element reference for scrollable container"
      },
      "direction": {
        "type": "string",
        "enum": ["up", "down", "left", "right"],
        "description": "Scroll direction"
      },
      "amount": {
        "type": "string",
        "enum": ["small", "large", "page"],
        "default": "small",
        "description": "Scroll amount"
      }
    },
    "required": ["element", "ref", "direction"]
  }
}
```

#### `wpf_scroll_into_view`
**Description**: Scroll element into visible area.

```json
{
  "name": "wpf_scroll_into_view",
  "description": "Scrolls element into the visible viewport",
  "inputSchema": {
    "type": "object",
    "properties": {
      "element": {
        "type": "string",
        "description": "Human-readable element description"
      },
      "ref": {
        "type": "string",
        "description": "Element reference to scroll into view"
      }
    },
    "required": ["element", "ref"]
  }
}
```

#### `wpf_focus`
**Description**: Set focus to an element.

```json
{
  "name": "wpf_focus",
  "description": "Sets keyboard focus to an element",
  "inputSchema": {
    "type": "object",
    "properties": {
      "element": {
        "type": "string",
        "description": "Human-readable element description"
      },
      "ref": {
        "type": "string",
        "description": "Element reference to focus"
      }
    },
    "required": ["element", "ref"]
  }
}
```

### 4.5 Window Management Tools

#### `wpf_list_windows`
**Description**: List all windows of the attached application.

```json
{
  "name": "wpf_list_windows",
  "description": "Lists all windows belonging to the application",
  "inputSchema": {
    "type": "object",
    "properties": {}
  }
}
```

#### `wpf_switch_window`
**Description**: Switch to a different window.

```json
{
  "name": "wpf_switch_window",
  "description": "Switches focus to a different application window",
  "inputSchema": {
    "type": "object",
    "properties": {
      "window_ref": {
        "type": "string",
        "description": "Window reference from wpf_list_windows"
      },
      "window_title": {
        "type": "string",
        "description": "Window title to switch to"
      }
    }
  }
}
```

#### `wpf_window_action`
**Description**: Perform window-level actions.

```json
{
  "name": "wpf_window_action",
  "description": "Performs window-level actions like minimize, maximize, restore",
  "inputSchema": {
    "type": "object",
    "properties": {
      "action": {
        "type": "string",
        "enum": ["minimize", "maximize", "restore", "close"],
        "description": "Window action to perform"
      },
      "window_ref": {
        "type": "string",
        "description": "Window reference (default: main window)"
      }
    },
    "required": ["action"]
  }
}
```

### 4.6 Utility Tools

#### `wpf_take_screenshot`
**Description**: Capture screenshot of window or element.

```json
{
  "name": "wpf_take_screenshot",
  "description": "Captures a screenshot and returns as base64",
  "inputSchema": {
    "type": "object",
    "properties": {
      "ref": {
        "type": "string",
        "description": "Element reference to capture (default: main window)"
      },
      "format": {
        "type": "string",
        "enum": ["png", "jpeg"],
        "default": "png",
        "description": "Image format"
      }
    }
  }
}
```

#### `wpf_wait_for`
**Description**: Wait for condition to be met.

```json
{
  "name": "wpf_wait_for",
  "description": "Waits for an element condition to be met",
  "inputSchema": {
    "type": "object",
    "properties": {
      "ref": {
        "type": "string",
        "description": "Element reference to wait for"
      },
      "condition": {
        "type": "string",
        "enum": ["visible", "enabled", "focused", "exists", "not_exists"],
        "description": "Condition to wait for"
      },
      "timeout_ms": {
        "type": "integer",
        "default": 10000,
        "minimum": 100,
        "maximum": 60000,
        "description": "Maximum wait time in milliseconds (100ms - 60s)"
      }
    },
    "required": ["condition"]
  }
}
```

### 4.7 Edge Case Behaviors

| Tool | Edge Case | Expected Behavior |
|------|-----------|-------------------|
| `wpf_type` | Empty string `""` | Clears field if `clear_first=true`, no-op otherwise |
| `wpf_type` | Text > 10,000 chars | Returns `VALUE_TOO_LONG` error with 10KB limit |
| `wpf_type` | Unicode/emoji | Fully supported via keyboard simulation |
| `wpf_click` | Disabled element | Returns `ELEMENT_NOT_ENABLED` error |
| `wpf_click` | Invisible element | Returns `ELEMENT_NOT_VISIBLE` error |
| `wpf_set_value` | Read-only element | Returns `ELEMENT_READ_ONLY` error |
| `wpf_snapshot` | 1000+ elements | Performance warning in metadata; consider `max_depth` reduction |
| `wpf_wait_for` | Already satisfied | Returns immediately with success |
| `wpf_select` | Item not found | Returns `ITEM_NOT_FOUND` with available items list |
| Any tool | Invalid `ref` | Returns `ELEMENT_NOT_FOUND` with suggestion to refresh |
| Any tool | Negative timeout | Returns `INVALID_PARAMETER` error |

---

## 5. Performance Requirements

### 5.1 Performance Baselines

| Operation | Element Count | Target P95 | Target P99 |
|-----------|---------------|------------|------------|
| `wpf_snapshot` | ≤100 elements | 200ms | 500ms |
| `wpf_snapshot` | 100-500 elements | 500ms | 1000ms |
| `wpf_snapshot` | 500-1000 elements | 1000ms | 2000ms |
| `wpf_click` (InvokePattern) | N/A | 100ms | 200ms |
| `wpf_click` (mouse simulation) | N/A | 300ms | 500ms |
| `wpf_type` | ≤100 characters | 500ms | 1000ms |
| `wpf_set_value` | N/A | 100ms | 200ms |
| `wpf_find_element` | N/A | 200ms | 500ms |
| `wpf_get_element_properties` | N/A | 50ms | 100ms |

### 5.2 Quality Requirements

| Requirement | Target | Measurement |
|-------------|--------|-------------|
| Operation success rate | > 95% | Successful tool invocations / total |
| Error clarity | 100% actionable errors | All errors include fix guidance |
| Memory stability | No leaks over 1hr session | Memory profiling |
| Crash recovery | Graceful degradation | No unhandled exceptions |

### 5.3 Usability Requirements

| Requirement | Target | Measurement |
|-------------|--------|-------------|
| Setup time | < 5 minutes | Time to first successful interaction |
| Documentation completeness | 100% tools documented | Documented / total tools |
| Example coverage | 1+ example per tool | Examples in documentation |

---

## 6. Error Handling

### 6.1 Error Codes

| Code | Description | Suggested Action |
|------|-------------|------------------|
| `APP_NOT_ATTACHED` | No application attached | Call `wpf_launch_application` or `wpf_attach_application` |
| `ELEMENT_NOT_FOUND` | Element reference invalid | Call `wpf_snapshot` to refresh |
| `ELEMENT_STALE` | Element ref no longer valid | Call `wpf_snapshot` to get fresh references |
| `PATTERN_NOT_SUPPORTED` | Element doesn't support pattern | Use alternative tool or check element type |
| `TIMEOUT` | Operation timed out | Increase timeout or check application state |
| `ELEMENT_NOT_ENABLED` | Element is disabled | Wait for element to be enabled |
| `ELEMENT_NOT_VISIBLE` | Element is not visible | Scroll into view or check visibility |
| `ELEMENT_READ_ONLY` | Element cannot be modified | Check element state |
| `ELEMENT_DISAPPEARED` | Element removed during operation | Refresh snapshot |
| `APP_CRASHED` | Application terminated unexpectedly | Restart application |
| `APP_NOT_RESPONDING` | Application is hung | Wait or force-close |
| `VALUE_TOO_LONG` | Input text exceeds 10KB limit | Reduce text length |
| `INVALID_PARAMETER` | Invalid parameter value | Check parameter constraints |
| `ITEM_NOT_FOUND` | Selection item not found | Check available items |

### 6.2 Failure Mode Handling

| Failure Mode | Detection | Response | Recovery |
|--------------|-----------|----------|----------|
| **Application crash** | `Process.HasExited` check | Return `APP_CRASHED` error; clear all element refs; set state to disconnected | User must call `wpf_launch_application` or `wpf_attach_application` |
| **Stale element reference** | UIA exception on element access | Return `ELEMENT_STALE` error with refresh suggestion | Call `wpf_snapshot` to get fresh references (NO auto-retry) |
| **UI thread deadlock** | Operation timeout without response | Kill operation thread; return `TIMEOUT` error | Increase timeout or investigate app UI thread blocking |
| **Element disappeared mid-operation** | ElementNotAvailable exception | Return `ELEMENT_DISAPPEARED` error | Refresh snapshot; element may have been removed by app |
| **Pattern not available** | Pattern query returns null | Return `PATTERN_NOT_SUPPORTED` with available patterns | Use alternative interaction method |
| **Application not responding** | Win32 IsHungAppWindow check | Return `APP_NOT_RESPONDING` warning | Wait or force-close based on user preference |

**Important**: The server does NOT implement automatic retries for failed operations. State changes between failure and retry make automatic recovery unreliable. The AI agent should explicitly request retry after receiving error information.

---

## 7. Dependencies

### 7.1 External Dependencies

| Dependency | Version | Purpose |
|------------|---------|---------|
| .NET SDK | 8.0+ | Runtime and build |
| ModelContextProtocol | Latest preview | MCP server framework |
| FlaUI.Core | 5.0+ | UI Automation abstraction |
| FlaUI.UIA3 | 5.0+ | UIA3 implementation |
| Microsoft.Extensions.Hosting | 8.0+ | Host builder and DI |

### 7.2 Development Dependencies

| Dependency | Purpose |
|------------|---------|
| Visual Studio 2022 / Rider | IDE |
| Git | Version control |
| Sample WPF Application | Testing target |

---

## 8. Accessibility Tree Format Specification

### 8.1 Format Goals
- Human-readable for LLM comprehension
- Compact for token efficiency
- Includes actionable element references
- Shows relevant state information

### 8.2 Element Format
```
- {controlType} "{name}" [ref={id}] [{states}]
```

### 8.3 State Indicators

| State | Indicator |
|-------|-----------|
| Enabled | `[enabled]` (default, usually omitted) |
| Disabled | `[disabled]` |
| Focused | `[focused]` |
| Selected | `[selected]` |
| Expanded | `[expanded]` |
| Collapsed | `[collapsed]` |
| Checked | `[checked]` |
| Unchecked | `[unchecked]` |
| Read-only | `[readonly]` |
| Has value | `[value="..."]` |
| Modal | `[modal]` |

### 8.4 Example Output
```yaml
- window "Customer Management" [ref=e1]
  - toolbar [ref=e2]
    - button "New" [ref=e3]
    - button "Save" [ref=e4] [disabled]
    - button "Delete" [ref=e5]
  - splitcontainer [ref=e6]
    - listbox "Customers" [ref=e7]
      - listitem "John Doe" [ref=e8] [selected]
      - listitem "Jane Smith" [ref=e9]
    - form [ref=e10]
      - textbox "First Name" [ref=e11] [value="John"]
      - textbox "Last Name" [ref=e12] [value="Doe"]
      - textbox "Email" [ref=e13] [value="john@example.com"]
      - combobox "Country" [ref=e14] [value="USA"]
      - checkbox "Active" [ref=e15] [checked]
  - statusbar "Ready" [ref=e16]
```

---

## 9. Sample WPF Application Requirements

For testing and validation, the target WPF application should include:

### 9.1 Required Controls
- Button (with and without AutomationId)
- TextBox (single and multi-line)
- CheckBox
- RadioButton
- ComboBox
- ListBox
- DataGrid
- TreeView
- Menu and MenuItems
- TabControl
- Expander
- ProgressBar
- Slider

### 9.2 Required Scenarios
- Login form workflow
- Data entry form
- Master-detail view
- Modal dialog handling
- Multi-window interaction

---

## 10. Acceptance Tests

| Test ID | Name | Precondition | Steps | Expected Result |
|---------|------|--------------|-------|-----------------|
| **AT-001** | Basic workflow completion | Sample WPF app available | 1. `wpf_launch_application` 2. `wpf_snapshot` 3. `wpf_click` on button 4. `wpf_type` in textbox | All operations succeed with latency within baselines |
| **AT-002** | Error recovery - invalid ref | Application attached | 1. `wpf_click(ref="invalid_ref")` | `ELEMENT_NOT_FOUND` error with suggestion to refresh snapshot |
| **AT-003** | Snapshot performance | App with 500 elements | 1. `wpf_snapshot` | Completes in < 1000ms (P95) |
| **AT-004** | Stale element handling | Valid snapshot exists | 1. UI changes externally 2. `wpf_click` on moved element | `ELEMENT_STALE` error with actionable guidance |
| **AT-005** | Login workflow | Login form visible | 1. `wpf_snapshot` 2. `wpf_type` username 3. `wpf_type` password 4. `wpf_click` login | Login succeeds; main window appears |
| **AT-006** | Modal dialog handling | Dialog can be triggered | 1. Trigger dialog 2. `wpf_list_windows` 3. `wpf_switch_window` 4. Interact with dialog | Dialog interaction completes successfully |
| **AT-007** | DataGrid navigation | DataGrid with 100+ rows | 1. `wpf_snapshot` 2. `wpf_scroll` down 3. `wpf_snapshot` | New rows visible in second snapshot |
| **AT-008** | Application crash recovery | App attached | 1. Force-close app externally 2. Any tool call | `APP_CRASHED` error returned; state properly reset |

---

## 11. Automation Readiness Checklist

### For WPF Application Developers

Use this checklist to ensure your WPF application is automation-ready:

#### Required (Critical)
- [ ] All buttons have `AutomationProperties.AutomationId`
- [ ] All text inputs have `AutomationProperties.AutomationId`
- [ ] All checkboxes/radio buttons have `AutomationProperties.AutomationId`
- [ ] All list controls (ComboBox, ListBox, DataGrid) have `AutomationProperties.AutomationId`
- [ ] Custom controls have proper `AutomationPeer` implementations

#### Recommended (High Value)
- [ ] Key elements have descriptive `AutomationProperties.Name`
- [ ] Form fields have associated labels (for accessibility)
- [ ] No operations block UI thread for > 2 seconds
- [ ] Modal dialogs are proper child windows (not overlays)
- [ ] DataGrid/ListView use virtualization for large datasets

#### XAML Examples
```xml
<!-- Good: Automation-ready button -->
<Button Content="Save"
        AutomationProperties.AutomationId="btnSave"
        AutomationProperties.Name="Save Document" />

<!-- Good: Automation-ready textbox with label -->
<Label Content="Username:" Target="{Binding ElementName=txtUsername}" />
<TextBox x:Name="txtUsername"
         AutomationProperties.AutomationId="txtUsername" />

<!-- Good: Custom control with AutomationPeer -->
<local:CustomDatePicker AutomationProperties.AutomationId="datePicker" />
```

---

*Document Version: 1.1 Final*
*Last Updated: 2026-01-23*
*Status: Expert Panel Reviewed - Ready for Implementation*
