# WPF-MCP Tools Reference

Complete documentation for all MCP tools provided by the WPF-MCP server.

## Table of Contents

- [Application Management](#application-management)
- [Element Discovery](#element-discovery)
- [UI Interaction](#ui-interaction)
- [Navigation](#navigation)
- [Window Management](#window-management)
- [Utilities](#utilities)
- [Error Codes](#error-codes)

---

## Application Management

### wpf_launch_application

Launches a WPF application executable and waits for the main window.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `path` | string | Yes | - | Full path to the WPF executable |
| `arguments` | string[] | No | null | Command line arguments |
| `timeout_ms` | int | No | 30000 | Max wait time for window (1000-120000) |

**Returns:**
```json
{
  "success": true,
  "data": {
    "window_title": "My Application",
    "process_id": 12345,
    "is_ready": true
  }
}
```

**Errors:**
- `FILE_NOT_FOUND` - Executable path doesn't exist
- `TIMEOUT` - Main window didn't appear in time
- `LAUNCH_FAILED` - Process failed to start

**Example:**
```json
{
  "path": "C:\\Program Files\\MyApp\\App.exe",
  "arguments": ["--config", "production"],
  "timeout_ms": 60000
}
```

---

### wpf_attach_application

Attaches to a running WPF application by process name or ID.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `process_name` | string | No* | null | Process name (without .exe) |
| `process_id` | int | No* | null | Process ID to attach to |

*At least one of `process_name` or `process_id` is required.

**Returns:**
```json
{
  "success": true,
  "data": {
    "window_title": "My Application",
    "process_id": 12345,
    "is_ready": true
  }
}
```

**Errors:**
- `INVALID_PARAMETER` - Neither name nor ID provided
- `APP_NOT_ATTACHED` - Process not found
- `TIMEOUT` - Window not available

**Example:**
```json
{
  "process_name": "notepad"
}
```

---

### wpf_close_application

Closes the currently attached WPF application.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `force` | bool | No | false | Force kill if graceful close fails |

**Returns:**
```json
{
  "success": true,
  "data": {
    "closed": true
  }
}
```

**Errors:**
- `APP_NOT_ATTACHED` - No application attached
- `APP_NOT_RESPONDING` - Failed to close gracefully

---

### wpf_set_background_mode

Enables or disables background automation mode. When enabled, only UI Automation patterns are used (no mouse/keyboard simulation), allowing the target application to stay in background without stealing focus.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `enabled` | bool | Yes | - | True to enable background mode |

**Returns:**
```json
{
  "success": true,
  "data": {
    "background_mode": true,
    "description": "Background mode enabled: Only UI Automation patterns will be used"
  }
}
```

**Notes:**
- In background mode, operations that require mouse/keyboard simulation will fail with `BACKGROUND_MODE_NOT_SUPPORTED`
- Pattern-based operations (InvokePattern, ValuePattern, TogglePattern, SelectionItemPattern) work in background
- Double-click, right-click, and keyboard shortcuts are NOT supported in background mode
- Individual tools have a `background` parameter to override the global setting

---

### wpf_get_background_mode

Gets the current background automation mode status.

**Returns:**
```json
{
  "success": true,
  "data": {
    "background_mode": false,
    "description": "Background mode disabled: Mouse and keyboard simulation allowed"
  }
}
```

---

## Element Discovery

### wpf_snapshot

Returns a structured accessibility tree snapshot for LLM analysis.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `root_ref` | string | No | null | Element ref to use as root (default: main window) |
| `max_depth` | int | No | 5 | Maximum tree depth (1-20) |
| `include_invisible` | bool | No | false | Include off-screen elements |

**Returns:**
```json
{
  "success": true,
  "data": {
    "element_count": 42,
    "snapshot": "- window \"My App\" [ref=e1]\n  - button \"Save\" [ref=e2] [enabled]\n  ..."
  }
}
```

**YAML Output Format:**
```yaml
- window "Main Window" [ref=e1]
  - pane "Content" [ref=e2]
    - button "Submit" [ref=e3] [enabled]
    - textbox "Email" [ref=e4] [value="user@example.com"] [focused]
    - checkbox "Remember Me" [ref=e5] [checked]
    - combobox "Country" [ref=e6] [collapsed]
```

**State Indicators:**
- `[enabled]` / `[disabled]`
- `[focused]`
- `[checked]` / `[unchecked]`
- `[selected]`
- `[expanded]` / `[collapsed]`
- `[readonly]`
- `[modal]`
- `[value="..."]`

**Errors:**
- `APP_NOT_ATTACHED` - No application attached
- `APP_CRASHED` - Application has terminated
- `INVALID_PARAMETER` - Invalid max_depth
- `ELEMENT_NOT_FOUND` - Root ref not found

**Notes:**
- Calling this invalidates all previous element references
- Large snapshots (>1000 elements) trigger a performance warning

---

### wpf_find_element

Find elements by AutomationId, Name, or ControlType.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `automation_id` | string | No* | null | Exact AutomationId match |
| `name` | string | No* | null | Partial name match (case-insensitive) |
| `control_type` | string | No* | null | Control type (Button, TextBox, etc.) |
| `root_ref` | string | No | null | Search within this element |

*At least one search criterion is required.

**Returns:**
```json
{
  "success": true,
  "data": {
    "count": 2,
    "elements": [
      {
        "ref": "e1",
        "control_type": "Button",
        "name": "Save",
        "automation_id": "btnSave",
        "is_enabled": true
      }
    ]
  }
}
```

**Example:**
```json
{
  "automation_id": "txtUsername"
}
```

---

### wpf_get_element_properties

Returns all automation properties for an element.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `ref` | string | Yes | - | Element reference from snapshot |

**Returns:**
```json
{
  "success": true,
  "data": {
    "ref": "e3",
    "name": "Submit",
    "automation_id": "btnSubmit",
    "control_type": "Button",
    "class_name": "Button",
    "is_enabled": true,
    "is_offscreen": false,
    "is_keyboard_focusable": true,
    "has_keyboard_focus": false,
    "process_id": 12345,
    "framework_id": "WPF",
    "bounding_rectangle": {
      "x": 100,
      "y": 200,
      "width": 80,
      "height": 30
    },
    "supported_patterns": ["Invoke", "ExpandCollapse"]
  }
}
```

---

## UI Interaction

### wpf_click

Clicks an element using InvokePattern or mouse simulation.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `element` | string | Yes | - | Human-readable description |
| `ref` | string | Yes | - | Element reference from snapshot |
| `click_type` | string | No | "single" | Type: single, double, or right |
| `background` | bool | No | null | Override global background mode (true=patterns only) |

**Returns:**
```json
{
  "success": true,
  "data": {
    "clicked": true,
    "element_description": "Save button",
    "click_type": "single"
  }
}
```

**Errors:**
- `ELEMENT_NOT_ENABLED` - Element is disabled
- `ELEMENT_NOT_VISIBLE` - Element is off-screen
- `ELEMENT_DISAPPEARED` - Element no longer exists

**Notes:**
- Uses InvokePattern first (faster, more reliable)
- Falls back to mouse simulation if pattern unavailable
- For off-screen elements, call `wpf_scroll_into_view` first

---

### wpf_type

Types text into a text input element.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `element` | string | Yes | - | Human-readable description |
| `ref` | string | Yes | - | Element reference from snapshot |
| `text` | string | Yes | - | Text to type (max 10,000 chars) |
| `clear_first` | bool | No | true | Clear existing text first |
| `background` | bool | No | null | Override global background mode (true=ValuePattern only) |

**Returns:**
```json
{
  "success": true,
  "data": {
    "typed": true,
    "element_description": "Username field",
    "text_length": 15,
    "cleared_first": true
  }
}
```

**Errors:**
- `VALUE_TOO_LONG` - Text exceeds 10KB limit
- `ELEMENT_NOT_ENABLED` - Element is disabled
- `ELEMENT_READ_ONLY` - Element is read-only

---

### wpf_set_value

Sets value directly using ValuePattern (faster than typing).

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `element` | string | Yes | - | Human-readable description |
| `ref` | string | Yes | - | Element reference from snapshot |
| `value` | string | Yes | - | Value to set (max 10,000 chars) |

**Returns:**
```json
{
  "success": true,
  "data": {
    "value_set": true,
    "element_description": "Email input"
  }
}
```

**Errors:**
- `PATTERN_NOT_SUPPORTED` - Element doesn't support ValuePattern
- `ELEMENT_READ_ONLY` - Element is read-only

**Notes:**
- Faster than `wpf_type` but doesn't trigger keypress events
- Use `wpf_type` if the app validates on keystroke

---

### wpf_toggle

Toggles a checkbox or toggle button.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `element` | string | Yes | - | Human-readable description |
| `ref` | string | Yes | - | Element reference from snapshot |
| `target_state` | string | No | "toggle" | Desired state: on, off, or toggle |

**Returns:**
```json
{
  "success": true,
  "data": {
    "toggled": true,
    "element_description": "Remember me checkbox",
    "previous_state": "off",
    "new_state": "on"
  }
}
```

**Errors:**
- `PATTERN_NOT_SUPPORTED` - Element doesn't support TogglePattern
- `ELEMENT_NOT_ENABLED` - Element is disabled

---

### wpf_select

Selects an item in a selection control (ComboBox, ListBox, etc.).

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `element` | string | Yes | - | Human-readable description |
| `ref` | string | Yes | - | Container element reference |
| `item` | string | No* | null | Item text to select (partial match) |
| `item_ref` | string | No* | null | Direct reference to item element |
| `background` | bool | No | null | Override global background mode |

*At least one of `item` or `item_ref` is required.

**Returns:**
```json
{
  "success": true,
  "data": {
    "selected": true,
    "element_description": "Country dropdown",
    "selected_item": "United States"
  }
}
```

**Errors:**
- `ITEM_NOT_FOUND` - Item not found (includes available items in message)
- `INVALID_PARAMETER` - Neither item nor item_ref provided

---

### wpf_expand_collapse

Expands or collapses an expandable element.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `element` | string | Yes | - | Human-readable description |
| `ref` | string | Yes | - | Element reference from snapshot |
| `action` | string | No | "toggle" | Action: expand, collapse, or toggle |

**Returns:**
```json
{
  "success": true,
  "data": {
    "action_performed": "expand",
    "element_description": "Settings panel",
    "previous_state": "collapsed",
    "new_state": "expanded"
  }
}
```

---

### wpf_press_key

Sends keyboard key press to the focused element.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `key` | string | Yes | - | Key to press (see supported keys) |
| `ref` | string | No | null | Element to focus first |
| `background` | bool | No | null | Override global background mode (keyboard never works in background) |

**Note:** This tool always fails in background mode as keyboard simulation requires foreground focus.

**Supported Keys:**
- Letters: `A-Z`
- Numbers: `0-9`
- Function keys: `F1-F12`
- Navigation: `Up`, `Down`, `Left`, `Right`, `Home`, `End`, `PageUp`, `PageDown`
- Special: `Enter`, `Tab`, `Escape`, `Space`, `Backspace`, `Delete`, `Insert`
- Modifiers: `Ctrl`, `Alt`, `Shift`, `Win`
- Combinations: `Ctrl+S`, `Ctrl+Shift+N`, `Alt+F4`

**Returns:**
```json
{
  "success": true,
  "data": {
    "key_pressed": "Ctrl+S",
    "focused_ref": "e3"
  }
}
```

**Example:**
```json
{
  "key": "Ctrl+A",
  "ref": "e5"
}
```

---

## Navigation

### wpf_scroll

Scrolls within a scrollable element.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `element` | string | Yes | - | Human-readable description |
| `ref` | string | Yes | - | Scrollable container reference |
| `direction` | string | Yes | - | Direction: up, down, left, right |
| `amount` | string | No | "small" | Amount: small, large, or page |

**Returns:**
```json
{
  "success": true,
  "data": {
    "scrolled": true,
    "element_description": "Message list",
    "direction": "down",
    "amount": "page",
    "horizontal_scroll_percent": 0,
    "vertical_scroll_percent": 45.5
  }
}
```

**Errors:**
- `PATTERN_NOT_SUPPORTED` - Element doesn't support ScrollPattern

---

### wpf_scroll_into_view

Scrolls an element into the visible viewport.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `element` | string | Yes | - | Human-readable description |
| `ref` | string | Yes | - | Element reference to scroll into view |

**Returns:**
```json
{
  "success": true,
  "data": {
    "scrolled_into_view": true,
    "element_description": "Row 50 in table",
    "is_offscreen": false
  }
}
```

**Notes:**
- Uses ScrollItemPattern if available
- Falls back to focusing the element

---

### wpf_focus

Sets keyboard focus to an element.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `element` | string | Yes | - | Human-readable description |
| `ref` | string | Yes | - | Element reference from snapshot |

**Returns:**
```json
{
  "success": true,
  "data": {
    "focused": true,
    "element_description": "Search input",
    "has_keyboard_focus": true
  }
}
```

**Errors:**
- `PATTERN_NOT_SUPPORTED` - Element cannot receive focus

---

## Window Management

### wpf_list_windows

Lists all windows belonging to the attached application.

**Parameters:** None

**Returns:**
```json
{
  "success": true,
  "data": {
    "count": 2,
    "windows": [
      {
        "ref": "e1",
        "title": "Main Window",
        "is_modal": false,
        "is_main": true,
        "window_state": "normal"
      },
      {
        "ref": "e15",
        "title": "Settings",
        "is_modal": true,
        "is_main": false,
        "window_state": "normal"
      }
    ]
  }
}
```

---

### wpf_switch_window

Switches focus to a different application window.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `window_ref` | string | No* | null | Window reference from wpf_list_windows |
| `window_title` | string | No* | null | Partial window title match |

*At least one of `window_ref` or `window_title` is required.

**Returns:**
```json
{
  "success": true,
  "data": {
    "switched": true,
    "window_title": "Settings"
  }
}
```

---

### wpf_window_action

Performs window-level actions.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `action` | string | Yes | - | Action: minimize, maximize, restore, close |
| `window_ref` | string | No | null | Window reference (default: main window) |

**Returns:**
```json
{
  "success": true,
  "data": {
    "action_performed": "maximize",
    "window_title": "My Application"
  }
}
```

---

## Utilities

### wpf_take_screenshot

Captures a screenshot and returns as base64.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `ref` | string | No | null | Element to capture (default: main window) |
| `format` | string | No | "png" | Image format: png or jpeg |

**Returns:**
```json
{
  "success": true,
  "data": {
    "format": "png",
    "width": 1920,
    "height": 1080,
    "base64_length": 245678,
    "image_data": "iVBORw0KGgoAAAANSUhEUgAA..."
  }
}
```

---

### wpf_wait_for

Waits for an element condition to be met.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `ref` | string | No | null | Element reference (default: main window) |
| `condition` | string | No | "visible" | Condition: visible, enabled, focused, exists, not_exists |
| `timeout_ms` | int | No | 10000 | Maximum wait time (100-60000) |

**Returns:**
```json
{
  "success": true,
  "data": {
    "condition_met": true,
    "condition": "enabled",
    "wait_time_ms": 1250
  }
}
```

**Errors:**
- `TIMEOUT` - Condition not met within timeout

---

## Error Codes

| Code | Description | Common Causes |
|------|-------------|---------------|
| `APP_NOT_ATTACHED` | No application is connected | Call launch or attach first |
| `APP_CRASHED` | Application process has exited | Relaunch the application |
| `APP_NOT_RESPONDING` | UI thread is blocked | App may be hung; try waiting |
| `ELEMENT_NOT_FOUND` | Element reference not found | Reference invalid; refresh snapshot |
| `ELEMENT_STALE` | Element reference is outdated | UI changed; refresh snapshot |
| `ELEMENT_NOT_ENABLED` | Element is disabled | Wait for element to be enabled |
| `ELEMENT_NOT_VISIBLE` | Element is off-screen | Scroll element into view |
| `ELEMENT_READ_ONLY` | Element cannot be modified | Check element state |
| `ELEMENT_DISAPPEARED` | Element removed from UI | Refresh snapshot |
| `PATTERN_NOT_SUPPORTED` | Automation pattern unavailable | Use alternative tool |
| `TIMEOUT` | Operation exceeded time limit | Increase timeout or check app |
| `VALUE_TOO_LONG` | Input exceeds 10KB limit | Split into smaller chunks |
| `ITEM_NOT_FOUND` | Selection item not found | Check available items |
| `WINDOW_NOT_FOUND` | Window doesn't exist | Refresh window list |
| `FILE_NOT_FOUND` | Executable path invalid | Verify file path |
| `LAUNCH_FAILED` | Process failed to start | Check permissions |
| `INVALID_PARAMETER` | Bad input parameter | Check parameter values |
| `BACKGROUND_MODE_NOT_SUPPORTED` | Operation requires foreground | Disable background mode or use pattern-based alternative |

---

## Common Workflows

### Login Form Automation

```
1. wpf_launch_application(path="C:\\MyApp\\App.exe")
2. wpf_snapshot()
3. wpf_type(element="Username", ref="e3", text="user@example.com")
4. wpf_type(element="Password", ref="e4", text="password123")
5. wpf_click(element="Login button", ref="e5")
6. wpf_wait_for(condition="exists", timeout_ms=5000)
7. wpf_snapshot()
```

### Form Fill with Validation

```
1. wpf_snapshot()
2. wpf_set_value(element="Name", ref="e2", value="John Doe")
3. wpf_select(element="Country", ref="e3", item="United States")
4. wpf_toggle(element="Terms checkbox", ref="e4", target_state="on")
5. wpf_click(element="Submit", ref="e5")
```

### Multi-Window Application

```
1. wpf_list_windows()
2. wpf_switch_window(window_title="Settings")
3. wpf_snapshot()
4. wpf_toggle(element="Dark mode", ref="e3", target_state="on")
5. wpf_click(element="Save", ref="e4")
6. wpf_switch_window(window_title="Main")
```
