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
/// MCP tools for window management operations.
/// </summary>
[McpServerToolType]
public sealed class WpfWindowTools
{
    private readonly IApplicationManager _applicationManager;
    private readonly IElementReferenceManager _elementReferenceManager;

    public WpfWindowTools(IApplicationManager applicationManager, IElementReferenceManager elementReferenceManager)
    {
        _applicationManager = applicationManager;
        _elementReferenceManager = elementReferenceManager;
    }

    [McpServerTool(Name = "wpf_list_windows"), Description("Lists all windows belonging to the application")]
    public string ListWindows()
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

            if (_applicationManager.HasApplicationCrashed())
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.AppCrashed,
                    "The attached application has terminated",
                    "Call wpf_launch_application or wpf_attach_application to connect to a new application"));
            }

            var windows = _applicationManager.GetAllWindows();

            var windowList = windows.Select((w, index) =>
            {
                // Register each window to get a reference
                var reference = _elementReferenceManager.RegisterElement(w);
                return new
                {
                    @ref = reference.Ref,
                    title = w.Title,
                    is_modal = w.Patterns.Window.IsSupported && w.Patterns.Window.Pattern.IsModal.Value,
                    is_main = index == 0,
                    window_state = GetWindowState(w)
                };
            }).ToList();

            return JsonSerializer.Serialize(ToolResponse<object>.Ok(new
            {
                count = windowList.Count,
                windows = windowList
            }, new ResponseMetadata
            {
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            }));
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.AppNotResponding,
                $"Failed to list windows: {ex.Message}",
                "The application may not be responding"));
        }
    }

    [McpServerTool(Name = "wpf_switch_window"), Description("Switches focus to a different application window")]
    public string SwitchWindow(
        [Description("Window reference from wpf_list_windows")] string? window_ref = null,
        [Description("Window title to switch to")] string? window_title = null)
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

            if (string.IsNullOrEmpty(window_ref) && string.IsNullOrEmpty(window_title))
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.InvalidParameter,
                    "Either window_ref or window_title must be provided",
                    "Call wpf_list_windows to see available windows"));
            }

            Window? targetWindow = null;

            if (!string.IsNullOrEmpty(window_ref))
            {
                var element = _elementReferenceManager.GetElement(window_ref);
                if (element is Window window)
                {
                    targetWindow = window;
                }
                else
                {
                    return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                        ErrorCodes.WindowNotFound,
                        $"Window with ref '{window_ref}' not found",
                        "Call wpf_list_windows to get valid window references"));
                }
            }
            else if (!string.IsNullOrEmpty(window_title))
            {
                var windows = _applicationManager.GetAllWindows();
                targetWindow = windows.FirstOrDefault(w =>
                    w.Title != null && w.Title.Contains(window_title, StringComparison.OrdinalIgnoreCase));

                if (targetWindow == null)
                {
                    var availableTitles = windows.Select(w => w.Title).Where(t => !string.IsNullOrEmpty(t)).ToList();
                    return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                        ErrorCodes.WindowNotFound,
                        $"Window with title containing '{window_title}' not found",
                        $"Available windows: {string.Join(", ", availableTitles)}"));
                }
            }

            // Set focus to the window
            targetWindow!.SetForeground();
            targetWindow.Focus();

            return JsonSerializer.Serialize(ToolResponse<object>.Ok(new
            {
                switched = true,
                window_title = targetWindow.Title
            }, new ResponseMetadata
            {
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            }));
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.WindowNotFound,
                $"Failed to switch window: {ex.Message}",
                "The window may have been closed"));
        }
    }

    [McpServerTool(Name = "wpf_window_action"), Description("Performs window-level actions like minimize, maximize, restore")]
    public string WindowAction(
        [Description("Window action to perform: minimize, maximize, restore, or close")] string action,
        [Description("Window reference (default: main window)")] string? window_ref = null)
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

            Window? targetWindow;

            if (!string.IsNullOrEmpty(window_ref))
            {
                var element = _elementReferenceManager.GetElement(window_ref);
                if (element is Window window)
                {
                    targetWindow = window;
                }
                else
                {
                    return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                        ErrorCodes.WindowNotFound,
                        $"Window with ref '{window_ref}' not found",
                        "Call wpf_list_windows to get valid window references"));
                }
            }
            else
            {
                targetWindow = _applicationManager.MainWindow;
                if (targetWindow == null)
                {
                    return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                        ErrorCodes.WindowNotFound,
                        "No main window available",
                        "Call wpf_list_windows to see available windows"));
                }
            }

            // Check if WindowPattern is supported
            if (!targetWindow.Patterns.Window.IsSupported)
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.PatternNotSupported,
                    "Window does not support window operations",
                    "This may not be a standard window"));
            }

            var windowPattern = targetWindow.Patterns.Window.Pattern;

            switch (action.ToLowerInvariant())
            {
                case "minimize":
                    windowPattern.SetWindowVisualState(WindowVisualState.Minimized);
                    break;
                case "maximize":
                    windowPattern.SetWindowVisualState(WindowVisualState.Maximized);
                    break;
                case "restore" or "normal":
                    windowPattern.SetWindowVisualState(WindowVisualState.Normal);
                    break;
                case "close":
                    windowPattern.Close();
                    break;
                default:
                    return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                        ErrorCodes.InvalidParameter,
                        $"Invalid action: {action}",
                        "Use 'minimize', 'maximize', 'restore', or 'close'"));
            }

            return JsonSerializer.Serialize(ToolResponse<object>.Ok(new
            {
                action_performed = action,
                window_title = targetWindow.Title
            }, new ResponseMetadata
            {
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            }));
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.WindowNotFound,
                $"Failed to perform window action: {ex.Message}",
                "The window may have been closed"));
        }
    }

    private static string GetWindowState(Window window)
    {
        try
        {
            if (!window.Patterns.Window.IsSupported) return "unknown";

            var state = window.Patterns.Window.Pattern.WindowVisualState.Value;
            return state switch
            {
                WindowVisualState.Minimized => "minimized",
                WindowVisualState.Maximized => "maximized",
                WindowVisualState.Normal => "normal",
                _ => "unknown"
            };
        }
        catch
        {
            return "unknown";
        }
    }
}
