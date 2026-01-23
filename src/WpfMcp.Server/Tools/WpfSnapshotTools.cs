using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using ModelContextProtocol.Server;
using WpfMcp.Server.Models;
using WpfMcp.Server.Services;

namespace WpfMcp.Server.Tools;

/// <summary>
/// MCP tools for element discovery and snapshot operations.
/// </summary>
[McpServerToolType]
public sealed class WpfSnapshotTools
{
    private readonly IApplicationManager _applicationManager;
    private readonly IElementReferenceManager _elementReferenceManager;

    public WpfSnapshotTools(IApplicationManager applicationManager, IElementReferenceManager elementReferenceManager)
    {
        _applicationManager = applicationManager;
        _elementReferenceManager = elementReferenceManager;
    }

    [McpServerTool(Name = "wpf_snapshot"), Description("Returns structured accessibility tree snapshot for LLM analysis")]
    public string TakeSnapshot(
        [Description("Element reference to use as root (default: main window)")] string? root_ref = null,
        [Description("Maximum tree depth to traverse (1-20)")] int max_depth = 5,
        [Description("Include invisible elements in snapshot")] bool include_invisible = false)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!_applicationManager.IsAttached)
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.AppNotAttached,
                    "No application is currently attached",
                    "Call wpf_launch_application or wpf_attach_application first"));
            }

            // Validate max_depth
            if (max_depth < 1 || max_depth > 20)
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.InvalidParameter,
                    "max_depth must be between 1 and 20",
                    "Use a depth value that balances coverage and performance"));
            }

            // Check for application crash
            if (_applicationManager.HasApplicationCrashed())
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.AppCrashed,
                    "The attached application has terminated",
                    "Call wpf_launch_application or wpf_attach_application to connect to a new application"));
            }

            // Start a new snapshot context
            _elementReferenceManager.BeginNewSnapshot();

            // Get root element
            AutomationElement rootElement;
            if (!string.IsNullOrEmpty(root_ref))
            {
                var element = _elementReferenceManager.GetElement(root_ref);
                if (element == null)
                {
                    return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                        ErrorCodes.ElementNotFound,
                        $"Element with ref '{root_ref}' not found",
                        "Call wpf_snapshot without root_ref to refresh element references"));
                }
                rootElement = element;
            }
            else
            {
                rootElement = _applicationManager.MainWindow!;
            }

            // Build the snapshot tree
            var snapshotElement = BuildSnapshotTree(rootElement, 0, max_depth, include_invisible);

            var metadata = new ResponseMetadata
            {
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                SnapshotValid = true
            };

            // Add performance warning for large snapshots
            if (_elementReferenceManager.ElementCount > 1000)
            {
                metadata.Warnings.Add($"Large snapshot with {_elementReferenceManager.ElementCount} elements. Consider reducing max_depth for better performance.");
            }

            var result = new SnapshotResult
            {
                Tree = snapshotElement,
                ElementCount = _elementReferenceManager.ElementCount
            };

            // Return YAML format for better LLM readability
            var response = new
            {
                success = true,
                data = new
                {
                    element_count = result.ElementCount,
                    snapshot = result.Yaml
                },
                metadata = new
                {
                    execution_time_ms = metadata.ExecutionTimeMs,
                    warnings = metadata.Warnings,
                    snapshot_valid = metadata.SnapshotValid
                }
            };

            return JsonSerializer.Serialize(response);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.AppNotResponding,
                $"Failed to take snapshot: {ex.Message}",
                "The application may not be responding. Try again or check application state"));
        }
    }

    [McpServerTool(Name = "wpf_find_element"), Description("Find element by AutomationId, Name, or ControlType")]
    public string FindElement(
        [Description("Unique AutomationId property")] string? automation_id = null,
        [Description("Element Name property (visible text)")] string? name = null,
        [Description("UI Automation control type")] string? control_type = null,
        [Description("Search within this element (default: main window)")] string? root_ref = null)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!_applicationManager.IsAttached)
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.AppNotAttached,
                    "No application is currently attached",
                    "Call wpf_launch_application or wpf_attach_application first"));
            }

            if (string.IsNullOrEmpty(automation_id) && string.IsNullOrEmpty(name) && string.IsNullOrEmpty(control_type))
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.InvalidParameter,
                    "At least one search criterion must be provided",
                    "Specify automation_id, name, or control_type"));
            }

            // Get root element
            AutomationElement rootElement;
            if (!string.IsNullOrEmpty(root_ref))
            {
                var element = _elementReferenceManager.GetElement(root_ref);
                if (element == null)
                {
                    return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                        ErrorCodes.ElementNotFound,
                        $"Root element with ref '{root_ref}' not found",
                        "Call wpf_snapshot to refresh element references"));
                }
                rootElement = element;
            }
            else
            {
                rootElement = _applicationManager.MainWindow!;
            }

            // Find matching elements
            var matchingElements = FindMatchingElements(rootElement, automation_id, name, control_type);

            if (matchingElements.Count == 0)
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.ElementNotFound,
                    "No matching elements found",
                    "Verify search criteria or call wpf_snapshot to see available elements"));
            }

            // Register found elements and build results
            var results = matchingElements.Select(element =>
            {
                var reference = _elementReferenceManager.RegisterElement(element);
                return new
                {
                    @ref = reference.Ref,
                    control_type = reference.ControlType,
                    name = reference.Name,
                    automation_id = reference.AutomationId,
                    is_enabled = reference.IsEnabled
                };
            }).ToList();

            return JsonSerializer.Serialize(ToolResponse<object>.Ok(new
            {
                count = results.Count,
                elements = results
            }, new ResponseMetadata
            {
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                SnapshotValid = true
            }));
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.AppNotResponding,
                $"Failed to find element: {ex.Message}",
                "The application may not be responding"));
        }
    }

    [McpServerTool(Name = "wpf_get_element_properties"), Description("Returns all automation properties for an element")]
    public string GetElementProperties(
        [Description("Element reference from snapshot")] string @ref)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!_applicationManager.IsAttached)
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.AppNotAttached,
                    "No application is currently attached",
                    "Call wpf_launch_application or wpf_attach_application first"));
            }

            var element = _elementReferenceManager.GetElement(@ref);
            if (element == null)
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.ElementNotFound,
                    $"Element with ref '{@ref}' not found in current snapshot",
                    "Call wpf_snapshot to refresh element references"));
            }

            // Validate element is still accessible
            if (!_elementReferenceManager.IsReferenceValid(@ref))
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.ElementStale,
                    "Element reference is no longer valid",
                    "Call wpf_snapshot to get fresh element references"));
            }

            var properties = new Dictionary<string, object?>
            {
                ["ref"] = @ref,
                ["name"] = element.Properties.Name.ValueOrDefault,
                ["automation_id"] = element.Properties.AutomationId.ValueOrDefault,
                ["control_type"] = element.Properties.ControlType.ValueOrDefault.ToString(),
                ["class_name"] = element.Properties.ClassName.ValueOrDefault,
                ["is_enabled"] = element.Properties.IsEnabled.ValueOrDefault,
                ["is_offscreen"] = element.Properties.IsOffscreen.ValueOrDefault,
                ["is_keyboard_focusable"] = element.Properties.IsKeyboardFocusable.ValueOrDefault,
                ["has_keyboard_focus"] = element.Properties.HasKeyboardFocus.ValueOrDefault,
                ["process_id"] = element.Properties.ProcessId.ValueOrDefault,
                ["framework_id"] = element.Properties.FrameworkId.ValueOrDefault,
                ["bounding_rectangle"] = GetBoundingRectObject(element)
            };

            // Add pattern availability
            var patterns = GetSupportedPatterns(element);
            properties["supported_patterns"] = patterns;

            return JsonSerializer.Serialize(ToolResponse<object>.Ok(properties, new ResponseMetadata
            {
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            }));
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.ElementStale,
                $"Failed to get element properties: {ex.Message}",
                "The element may have been removed. Call wpf_snapshot to refresh"));
        }
    }

    private SnapshotElement BuildSnapshotTree(AutomationElement element, int currentDepth, int maxDepth, bool includeInvisible)
    {
        // Register the element
        var reference = _elementReferenceManager.RegisterElement(element);

        // Get states
        var states = GetElementStates(element);

        // Get value if available
        string? value = null;
        try
        {
            if (element.Patterns.Value.IsSupported)
            {
                value = element.Patterns.Value.Pattern.Value.Value;
            }
        }
        catch
        {
            // Value not available
        }

        var snapshotElement = new SnapshotElement
        {
            Ref = reference.Ref,
            ControlType = reference.ControlType,
            Name = reference.Name,
            AutomationId = reference.AutomationId,
            Value = value,
            States = states,
            Depth = currentDepth
        };

        // Add children if within depth limit
        if (currentDepth < maxDepth)
        {
            try
            {
                var children = element.FindAllChildren();
                foreach (var child in children)
                {
                    // Skip invisible elements if not requested
                    if (!includeInvisible && child.Properties.IsOffscreen.ValueOrDefault)
                    {
                        continue;
                    }

                    var childSnapshot = BuildSnapshotTree(child, currentDepth + 1, maxDepth, includeInvisible);
                    snapshotElement.Children.Add(childSnapshot);
                }
            }
            catch
            {
                // Children not accessible
            }
        }

        return snapshotElement;
    }

    private static List<string> GetElementStates(AutomationElement element)
    {
        var states = new List<string>();

        try
        {
            if (!element.Properties.IsEnabled.ValueOrDefault)
                states.Add("disabled");

            if (element.Properties.HasKeyboardFocus.ValueOrDefault)
                states.Add("focused");

            // Check toggle state
            if (element.Patterns.Toggle.IsSupported)
            {
                var toggleState = element.Patterns.Toggle.Pattern.ToggleState.Value;
                states.Add(toggleState == ToggleState.On ? "checked" : "unchecked");
            }

            // Check selection state
            if (element.Patterns.SelectionItem.IsSupported)
            {
                if (element.Patterns.SelectionItem.Pattern.IsSelected.Value)
                    states.Add("selected");
            }

            // Check expand/collapse state
            if (element.Patterns.ExpandCollapse.IsSupported)
            {
                var expandState = element.Patterns.ExpandCollapse.Pattern.ExpandCollapseState.Value;
                states.Add(expandState == ExpandCollapseState.Expanded ? "expanded" : "collapsed");
            }

            // Check if read-only
            if (element.Patterns.Value.IsSupported)
            {
                if (element.Patterns.Value.Pattern.IsReadOnly.Value)
                    states.Add("readonly");
            }

            // Check if modal
            if (element.Patterns.Window.IsSupported)
            {
                if (element.Patterns.Window.Pattern.IsModal.Value)
                    states.Add("modal");
            }
        }
        catch
        {
            // Some states not available
        }

        return states;
    }

    private static List<AutomationElement> FindMatchingElements(AutomationElement root, string? automationId, string? name, string? controlType)
    {
        var results = new List<AutomationElement>();

        void SearchRecursive(AutomationElement element)
        {
            bool matches = true;

            if (!string.IsNullOrEmpty(automationId))
            {
                matches &= element.Properties.AutomationId.ValueOrDefault == automationId;
            }

            if (!string.IsNullOrEmpty(name))
            {
                var elementName = element.Properties.Name.ValueOrDefault;
                matches &= elementName != null && elementName.Contains(name, StringComparison.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrEmpty(controlType))
            {
                var elementControlType = element.Properties.ControlType.ValueOrDefault.ToString();
                matches &= elementControlType.Equals(controlType, StringComparison.OrdinalIgnoreCase);
            }

            if (matches && (!string.IsNullOrEmpty(automationId) || !string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(controlType)))
            {
                results.Add(element);
            }

            try
            {
                foreach (var child in element.FindAllChildren())
                {
                    SearchRecursive(child);
                }
            }
            catch
            {
                // Children not accessible
            }
        }

        SearchRecursive(root);
        return results;
    }

    private static List<string> GetSupportedPatterns(AutomationElement element)
    {
        var patterns = new List<string>();

        if (element.Patterns.Invoke.IsSupported) patterns.Add("Invoke");
        if (element.Patterns.Value.IsSupported) patterns.Add("Value");
        if (element.Patterns.Toggle.IsSupported) patterns.Add("Toggle");
        if (element.Patterns.Selection.IsSupported) patterns.Add("Selection");
        if (element.Patterns.SelectionItem.IsSupported) patterns.Add("SelectionItem");
        if (element.Patterns.ExpandCollapse.IsSupported) patterns.Add("ExpandCollapse");
        if (element.Patterns.Scroll.IsSupported) patterns.Add("Scroll");
        if (element.Patterns.ScrollItem.IsSupported) patterns.Add("ScrollItem");
        if (element.Patterns.Grid.IsSupported) patterns.Add("Grid");
        if (element.Patterns.GridItem.IsSupported) patterns.Add("GridItem");
        if (element.Patterns.Table.IsSupported) patterns.Add("Table");
        if (element.Patterns.TableItem.IsSupported) patterns.Add("TableItem");
        if (element.Patterns.Transform.IsSupported) patterns.Add("Transform");
        if (element.Patterns.Window.IsSupported) patterns.Add("Window");

        return patterns;
    }

    private static object? GetBoundingRectObject(AutomationElement element)
    {
        try
        {
            var rect = element.Properties.BoundingRectangle.ValueOrDefault;
            if (rect.IsEmpty) return null;

            return new { x = rect.X, y = rect.Y, width = rect.Width, height = rect.Height };
        }
        catch
        {
            return null;
        }
    }
}
