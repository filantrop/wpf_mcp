using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using ModelContextProtocol.Server;
using WpfMcp.Server.Models;
using WpfMcp.Server.Services;

namespace WpfMcp.Server.Tools;

/// <summary>
/// MCP tools for utility operations like screenshots and waiting.
/// </summary>
[McpServerToolType]
public sealed class WpfUtilityTools
{
    private readonly IApplicationManager _applicationManager;
    private readonly IElementReferenceManager _elementReferenceManager;

    public WpfUtilityTools(IApplicationManager applicationManager, IElementReferenceManager elementReferenceManager)
    {
        _applicationManager = applicationManager;
        _elementReferenceManager = elementReferenceManager;
    }

    [McpServerTool(Name = "wpf_take_screenshot"), Description("Captures a screenshot and returns as base64")]
    public string TakeScreenshot(
        [Description("Element reference to capture (default: main window)")] string? @ref = null,
        [Description("Image format: png or jpeg")] string format = "png")
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

            AutomationElement targetElement;

            if (!string.IsNullOrEmpty(@ref))
            {
                var element = _elementReferenceManager.GetElement(@ref);
                if (element == null)
                {
                    return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                        ErrorCodes.ElementNotFound,
                        $"Element with ref '{@ref}' not found",
                        "Call wpf_snapshot to refresh element references"));
                }
                targetElement = element;
            }
            else
            {
                targetElement = _applicationManager.MainWindow!;
            }

            // Capture the screenshot
            var capture = Capture.Element(targetElement);

            // Validate format
            ImageFormat imageFormat = format.ToLowerInvariant() switch
            {
                "png" => ImageFormat.Png,
                "jpeg" or "jpg" => ImageFormat.Jpeg,
                _ => ImageFormat.Png
            };

            using var memoryStream = new MemoryStream();
            capture.Bitmap.Save(memoryStream, imageFormat);
            var base64 = Convert.ToBase64String(memoryStream.ToArray());

            return JsonSerializer.Serialize(ToolResponse<object>.Ok(new
            {
                format,
                width = capture.Bitmap.Width,
                height = capture.Bitmap.Height,
                base64_length = base64.Length,
                image_data = base64
            }, new ResponseMetadata
            {
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            }));
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.AppNotResponding,
                $"Failed to capture screenshot: {ex.Message}",
                "The element may not be visible or the application is not responding"));
        }
    }

    [McpServerTool(Name = "wpf_wait_for"), Description("Waits for an element condition to be met")]
    public async Task<string> WaitFor(
        [Description("Element reference to wait for")] string? @ref = null,
        [Description("Condition to wait for: visible, enabled, focused, exists, not_exists")] string condition = "visible",
        [Description("Maximum wait time in milliseconds (100ms - 60s)")] int timeout_ms = 10000)
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

            // Validate timeout
            if (timeout_ms < 100 || timeout_ms > 60000)
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.InvalidParameter,
                    "timeout_ms must be between 100 and 60000",
                    "Use a timeout value between 100ms and 60 seconds"));
            }

            // Validate condition
            var validConditions = new[] { "visible", "enabled", "focused", "exists", "not_exists" };
            if (!validConditions.Contains(condition.ToLowerInvariant()))
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.InvalidParameter,
                    $"Invalid condition: {condition}",
                    $"Use one of: {string.Join(", ", validConditions)}"));
            }

            using var cts = new CancellationTokenSource(timeout_ms);
            var conditionMet = false;

            while (!cts.Token.IsCancellationRequested)
            {
                conditionMet = EvaluateCondition(@ref, condition);

                if (conditionMet)
                {
                    break;
                }

                await Task.Delay(100, cts.Token);
            }

            if (conditionMet)
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Ok(new
                {
                    condition_met = true,
                    condition,
                    wait_time_ms = stopwatch.ElapsedMilliseconds
                }, new ResponseMetadata
                {
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                }));
            }
            else
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.Timeout,
                    $"Condition '{condition}' was not met within {timeout_ms}ms",
                    "Try increasing timeout_ms or check application state"));
            }
        }
        catch (OperationCanceledException)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.Timeout,
                $"Condition '{condition}' was not met within {timeout_ms}ms",
                "Try increasing timeout_ms or check application state"));
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.AppNotResponding,
                $"Failed to wait for condition: {ex.Message}",
                "The application may not be responding"));
        }
    }

    private bool EvaluateCondition(string? refId, string condition)
    {
        if (_applicationManager.HasApplicationCrashed())
        {
            return false;
        }

        // For exists/not_exists without specific ref, check if main window exists
        if (string.IsNullOrEmpty(refId))
        {
            var mainWindow = _applicationManager.MainWindow;
            if (mainWindow == null)
            {
                return condition == "not_exists";
            }

            return condition.ToLowerInvariant() switch
            {
                "exists" => true,
                "not_exists" => false,
                "visible" => !mainWindow.Properties.IsOffscreen.ValueOrDefault,
                "enabled" => mainWindow.Properties.IsEnabled.ValueOrDefault,
                "focused" => mainWindow.Properties.HasKeyboardFocus.ValueOrDefault,
                _ => false
            };
        }

        var element = _elementReferenceManager.GetElement(refId);

        if (element == null)
        {
            return condition == "not_exists";
        }

        try
        {
            return condition.ToLowerInvariant() switch
            {
                "exists" => true,
                "not_exists" => false,
                "visible" => !element.Properties.IsOffscreen.ValueOrDefault,
                "enabled" => element.Properties.IsEnabled.ValueOrDefault,
                "focused" => element.Properties.HasKeyboardFocus.ValueOrDefault,
                _ => false
            };
        }
        catch
        {
            // Element may have become stale
            return condition == "not_exists";
        }
    }
}
