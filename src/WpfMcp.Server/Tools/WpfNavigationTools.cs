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
/// MCP tools for navigation and scrolling operations.
/// </summary>
[McpServerToolType]
public sealed class WpfNavigationTools
{
    private readonly IApplicationManager _applicationManager;
    private readonly IElementReferenceManager _elementReferenceManager;

    public WpfNavigationTools(IApplicationManager applicationManager, IElementReferenceManager elementReferenceManager)
    {
        _applicationManager = applicationManager;
        _elementReferenceManager = elementReferenceManager;
    }

    [McpServerTool(Name = "wpf_scroll"), Description("Scrolls within a scrollable element")]
    public string Scroll(
        [Description("Human-readable element description")] string element,
        [Description("Element reference for scrollable container")] string @ref,
        [Description("Scroll direction: up, down, left, or right")] string direction,
        [Description("Scroll amount: small, large, or page")] string amount = "small")
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var validationResult = ValidateElementAccess(@ref, out var automationElement);
            if (validationResult != null) return validationResult;

            // Check if ScrollPattern is supported
            if (!automationElement!.Patterns.Scroll.IsSupported)
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.PatternNotSupported,
                    $"Element '{element}' does not support ScrollPattern",
                    "This element is not scrollable"));
            }

            var scrollPattern = automationElement.Patterns.Scroll.Pattern;

            // Determine scroll amounts
            ScrollAmount scrollAmount = amount.ToLowerInvariant() switch
            {
                "small" => ScrollAmount.SmallIncrement,
                "large" => ScrollAmount.LargeIncrement,
                "page" => ScrollAmount.LargeIncrement,
                _ => ScrollAmount.SmallIncrement
            };

            ScrollAmount horizontalAmount = ScrollAmount.NoAmount;
            ScrollAmount verticalAmount = ScrollAmount.NoAmount;

            switch (direction.ToLowerInvariant())
            {
                case "up":
                    verticalAmount = scrollAmount == ScrollAmount.SmallIncrement
                        ? ScrollAmount.SmallDecrement
                        : ScrollAmount.LargeDecrement;
                    break;
                case "down":
                    verticalAmount = scrollAmount;
                    break;
                case "left":
                    horizontalAmount = scrollAmount == ScrollAmount.SmallIncrement
                        ? ScrollAmount.SmallDecrement
                        : ScrollAmount.LargeDecrement;
                    break;
                case "right":
                    horizontalAmount = scrollAmount;
                    break;
                default:
                    return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                        ErrorCodes.InvalidParameter,
                        $"Invalid direction: {direction}",
                        "Use 'up', 'down', 'left', or 'right'"));
            }

            scrollPattern.Scroll(horizontalAmount, verticalAmount);

            return JsonSerializer.Serialize(ToolResponse<object>.Ok(new
            {
                scrolled = true,
                element_description = element,
                direction,
                amount,
                horizontal_scroll_percent = scrollPattern.HorizontalScrollPercent.Value,
                vertical_scroll_percent = scrollPattern.VerticalScrollPercent.Value
            }, new ResponseMetadata
            {
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            }));
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.ElementDisappeared,
                $"Failed to scroll: {ex.Message}",
                "The element may have been removed. Call wpf_snapshot to refresh"));
        }
    }

    [McpServerTool(Name = "wpf_scroll_into_view"), Description("Scrolls element into the visible viewport")]
    public string ScrollIntoView(
        [Description("Human-readable element description")] string element,
        [Description("Element reference to scroll into view")] string @ref)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var validationResult = ValidateElementAccess(@ref, out var automationElement);
            if (validationResult != null) return validationResult;

            // Check if ScrollItemPattern is supported
            if (automationElement!.Patterns.ScrollItem.IsSupported)
            {
                automationElement.Patterns.ScrollItem.Pattern.ScrollIntoView();
            }
            else
            {
                // Fall back to focusing the element
                automationElement.Focus();
            }

            return JsonSerializer.Serialize(ToolResponse<object>.Ok(new
            {
                scrolled_into_view = true,
                element_description = element,
                is_offscreen = automationElement.Properties.IsOffscreen.ValueOrDefault
            }, new ResponseMetadata
            {
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            }));
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.ElementDisappeared,
                $"Failed to scroll element into view: {ex.Message}",
                "The element may have been removed. Call wpf_snapshot to refresh"));
        }
    }

    [McpServerTool(Name = "wpf_focus"), Description("Sets keyboard focus to an element")]
    public string Focus(
        [Description("Human-readable element description")] string element,
        [Description("Element reference to focus")] string @ref)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var validationResult = ValidateElementAccess(@ref, out var automationElement);
            if (validationResult != null) return validationResult;

            // Check if element can receive focus
            if (!automationElement!.Properties.IsKeyboardFocusable.ValueOrDefault)
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.PatternNotSupported,
                    $"Element '{element}' cannot receive keyboard focus",
                    "This element is not focusable"));
            }

            automationElement.Focus();

            return JsonSerializer.Serialize(ToolResponse<object>.Ok(new
            {
                focused = true,
                element_description = element,
                has_keyboard_focus = automationElement.Properties.HasKeyboardFocus.ValueOrDefault
            }, new ResponseMetadata
            {
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            }));
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.ElementDisappeared,
                $"Failed to focus element: {ex.Message}",
                "The element may have been removed. Call wpf_snapshot to refresh"));
        }
    }

    private string? ValidateElementAccess(string refId, out AutomationElement? element)
    {
        element = null;

        if (!_applicationManager.IsAttached)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.AppNotAttached,
                "No application is currently attached",
                "Call wpf_launch_application or wpf_attach_application first"));
        }

        if (_applicationManager.HasApplicationCrashed())
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.AppCrashed,
                "The attached application has terminated",
                "Call wpf_launch_application or wpf_attach_application to connect to a new application"));
        }

        element = _elementReferenceManager.GetElement(refId);
        if (element == null)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.ElementNotFound,
                $"Element with ref '{refId}' not found in current snapshot",
                "Call wpf_snapshot to refresh element references"));
        }

        if (!_elementReferenceManager.IsReferenceValid(refId))
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.ElementStale,
                "Element reference is no longer valid",
                "Call wpf_snapshot to get fresh element references"));
        }

        return null;
    }
}
