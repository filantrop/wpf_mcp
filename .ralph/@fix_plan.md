# Ralph Fix Plan - WPF-MCP Server

## High Priority
- [ ] Build and verify project compiles successfully
- [ ] Implement wpf_take_screenshot System.Drawing dependency fix (may need System.Drawing.Common package)
- [ ] Add MCP server configuration (appsettings.json)

## Medium Priority
- [ ] Add integration tests with a sample WPF application
- [ ] Create sample WPF test application for acceptance tests
- [ ] Add configuration management for timeout defaults
- [ ] Improve error messages with more context

## Low Priority
- [ ] Performance optimization for large accessibility trees
- [ ] Add HTTP/SSE transport option
- [ ] Extended feature set (drag and drop, text selection, etc.)
- [ ] Advanced error recovery and retry logic

## Completed
- [x] Project initialization
- [x] Set up basic .NET 8 project structure (WpfMcp.sln, WpfMcp.Server.csproj, WpfMcp.Server.Tests.csproj)
- [x] Define core data structures and types (ElementReference, ToolResponse, SnapshotElement, ErrorCodes)
- [x] Implement MCP server foundation with tool registration (Program.cs)
- [x] Implement application management tools (wpf_launch_application, wpf_attach_application, wpf_close_application)
- [x] Implement element discovery tools (wpf_snapshot, wpf_find_element, wpf_get_element_properties)
- [x] Implement interaction tools (wpf_click, wpf_type, wpf_set_value, wpf_toggle, wpf_select, wpf_expand_collapse, wpf_press_key)
- [x] Implement navigation tools (wpf_scroll, wpf_scroll_into_view, wpf_focus)
- [x] Implement window management tools (wpf_list_windows, wpf_switch_window, wpf_window_action)
- [x] Implement utility tools (wpf_take_screenshot, wpf_wait_for)
- [x] Create core service interfaces and implementations (IApplicationManager, IElementReferenceManager)
- [x] Create basic unit tests for models and services

## Architecture Notes

### Project Structure
```
WPF-mcp/
├── WpfMcp.sln
├── src/
│   └── WpfMcp.Server/
│       ├── WpfMcp.Server.csproj
│       ├── Program.cs
│       ├── Models/
│       │   ├── ElementReference.cs
│       │   ├── ToolResponse.cs
│       │   ├── SnapshotElement.cs
│       │   └── ErrorCodes.cs
│       ├── Services/
│       │   ├── IApplicationManager.cs
│       │   ├── ApplicationManager.cs
│       │   ├── IElementReferenceManager.cs
│       │   └── ElementReferenceManager.cs
│       └── Tools/
│           ├── WpfApplicationTools.cs
│           ├── WpfSnapshotTools.cs
│           ├── WpfInteractionTools.cs
│           ├── WpfNavigationTools.cs
│           ├── WpfWindowTools.cs
│           └── WpfUtilityTools.cs
└── tests/
    └── WpfMcp.Server.Tests/
        ├── WpfMcp.Server.Tests.csproj
        ├── Models/
        │   ├── ToolResponseTests.cs
        │   └── SnapshotElementTests.cs
        └── Services/
            └── ElementReferenceManagerTests.cs
```

### MCP Tools Implemented
1. **Application Management**: wpf_launch_application, wpf_attach_application, wpf_close_application
2. **Element Discovery**: wpf_snapshot, wpf_find_element, wpf_get_element_properties
3. **Interaction**: wpf_click, wpf_type, wpf_set_value, wpf_toggle, wpf_select, wpf_expand_collapse, wpf_press_key
4. **Navigation**: wpf_scroll, wpf_scroll_into_view, wpf_focus
5. **Window Management**: wpf_list_windows, wpf_switch_window, wpf_window_action
6. **Utilities**: wpf_take_screenshot, wpf_wait_for

### Key Design Decisions
- Element references use format `e{n}` and are valid only within snapshot context
- All tools return standardized JSON response with success, data, error, and metadata
- Uses FlaUI.UIA3 for reliable WPF automation
- Supports both InvokePattern (fast) and mouse simulation (fallback) for clicks
- YAML-style output in wpf_snapshot for LLM readability

## Next Steps
1. Build the project to verify all code compiles
2. Fix any compilation errors
3. Run unit tests to verify basic functionality
4. Create a sample WPF application for integration testing
