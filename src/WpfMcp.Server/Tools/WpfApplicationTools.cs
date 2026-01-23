using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using ModelContextProtocol.Server;
using WpfMcp.Server.Models;
using WpfMcp.Server.Services;

namespace WpfMcp.Server.Tools;

/// <summary>
/// MCP tools for application management operations.
/// </summary>
[McpServerToolType]
public sealed class WpfApplicationTools
{
    private readonly IApplicationManager _applicationManager;
    private readonly IElementReferenceManager _elementReferenceManager;

    public WpfApplicationTools(IApplicationManager applicationManager, IElementReferenceManager elementReferenceManager)
    {
        _applicationManager = applicationManager;
        _elementReferenceManager = elementReferenceManager;
    }

    [McpServerTool(Name = "wpf_launch_application"), Description("Launches a WPF application executable and waits for main window")]
    public async Task<string> LaunchApplication(
        [Description("Full path to the WPF application executable")] string path,
        [Description("Command line arguments for the application")] string[]? arguments = null,
        [Description("Maximum time to wait for main window to appear (1-120 seconds)")] int timeout_ms = 30000)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate timeout
            if (timeout_ms < 1000 || timeout_ms > 120000)
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.InvalidParameter,
                    "timeout_ms must be between 1000 and 120000",
                    "Use a timeout value between 1 and 120 seconds"));
            }

            var mainWindow = await _applicationManager.LaunchApplicationAsync(path, arguments, timeout_ms);

            // Clear any previous element references
            _elementReferenceManager.Clear();

            var result = new
            {
                window_title = mainWindow.Title,
                process_id = mainWindow.Properties.ProcessId.Value,
                is_ready = true
            };

            return JsonSerializer.Serialize(ToolResponse<object>.Ok(result, new ResponseMetadata
            {
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            }));
        }
        catch (FileNotFoundException)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.FileNotFound,
                $"Application not found at path: {path}",
                "Verify the path exists and is accessible"));
        }
        catch (TimeoutException)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.Timeout,
                "Timed out waiting for application main window",
                "The application may be slow to start. Try increasing timeout_ms"));
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.LaunchFailed,
                $"Failed to launch application: {ex.Message}",
                "Check application path and permissions",
                recoverable: true));
        }
    }

    [McpServerTool(Name = "wpf_attach_application"), Description("Attaches to a running WPF application by process name or ID")]
    public async Task<string> AttachApplication(
        [Description("Name of the process (without .exe)")] string? process_name = null,
        [Description("Process ID to attach to")] int? process_id = null)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrEmpty(process_name) && !process_id.HasValue)
            {
                return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                    ErrorCodes.InvalidParameter,
                    "Either process_name or process_id must be provided",
                    "Specify process_name (e.g., 'notepad') or process_id (e.g., 12345)"));
            }

            FlaUI.Core.AutomationElements.Window mainWindow;

            if (process_id.HasValue)
            {
                mainWindow = await _applicationManager.AttachByIdAsync(process_id.Value);
            }
            else
            {
                mainWindow = await _applicationManager.AttachByNameAsync(process_name!);
            }

            // Clear any previous element references
            _elementReferenceManager.Clear();

            var result = new
            {
                window_title = mainWindow.Title,
                process_id = mainWindow.Properties.ProcessId.Value,
                is_ready = true
            };

            return JsonSerializer.Serialize(ToolResponse<object>.Ok(result, new ResponseMetadata
            {
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            }));
        }
        catch (ArgumentException ex)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.InvalidParameter,
                ex.Message,
                "Check process name or ID"));
        }
        catch (InvalidOperationException ex)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.AppNotAttached,
                ex.Message,
                "Verify the application is running"));
        }
        catch (TimeoutException)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.Timeout,
                "Timed out waiting for application window",
                "The application may not have a visible window"));
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.AppNotAttached,
                $"Failed to attach to application: {ex.Message}",
                "Verify the process exists and is accessible"));
        }
    }

    [McpServerTool(Name = "wpf_close_application"), Description("Closes the currently attached WPF application")]
    public async Task<string> CloseApplication(
        [Description("Force kill if graceful close fails")] bool force = false)
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

            await _applicationManager.CloseApplicationAsync(force);

            // Clear element references
            _elementReferenceManager.Clear();

            return JsonSerializer.Serialize(ToolResponse<object>.Ok(new
            {
                closed = true
            }, new ResponseMetadata
            {
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            }));
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(ToolResponse<object>.Fail(
                ErrorCodes.AppNotResponding,
                $"Failed to close application: {ex.Message}",
                "Try with force=true to force kill the application"));
        }
    }
}
