namespace WpfMcp.Server.Models;

/// <summary>
/// Standard error codes for WPF-MCP operations.
/// </summary>
public static class ErrorCodes
{
    /// <summary>No application is currently attached.</summary>
    public const string AppNotAttached = "APP_NOT_ATTACHED";

    /// <summary>Element reference not found in current snapshot.</summary>
    public const string ElementNotFound = "ELEMENT_NOT_FOUND";

    /// <summary>Element reference is stale (element changed since snapshot).</summary>
    public const string ElementStale = "ELEMENT_STALE";

    /// <summary>Element does not support the requested automation pattern.</summary>
    public const string PatternNotSupported = "PATTERN_NOT_SUPPORTED";

    /// <summary>Operation timed out.</summary>
    public const string Timeout = "TIMEOUT";

    /// <summary>Element is disabled and cannot be interacted with.</summary>
    public const string ElementNotEnabled = "ELEMENT_NOT_ENABLED";

    /// <summary>Element is not visible on screen.</summary>
    public const string ElementNotVisible = "ELEMENT_NOT_VISIBLE";

    /// <summary>Element is read-only and cannot be modified.</summary>
    public const string ElementReadOnly = "ELEMENT_READ_ONLY";

    /// <summary>Element disappeared during the operation.</summary>
    public const string ElementDisappeared = "ELEMENT_DISAPPEARED";

    /// <summary>The attached application has crashed.</summary>
    public const string AppCrashed = "APP_CRASHED";

    /// <summary>The application is not responding.</summary>
    public const string AppNotResponding = "APP_NOT_RESPONDING";

    /// <summary>Input value exceeds the maximum allowed length.</summary>
    public const string ValueTooLong = "VALUE_TOO_LONG";

    /// <summary>Invalid parameter value provided.</summary>
    public const string InvalidParameter = "INVALID_PARAMETER";

    /// <summary>Selection item not found in the control.</summary>
    public const string ItemNotFound = "ITEM_NOT_FOUND";

    /// <summary>The specified file or path was not found.</summary>
    public const string FileNotFound = "FILE_NOT_FOUND";

    /// <summary>Failed to launch the application.</summary>
    public const string LaunchFailed = "LAUNCH_FAILED";

    /// <summary>Window not found.</summary>
    public const string WindowNotFound = "WINDOW_NOT_FOUND";

    /// <summary>Operation not supported in background automation mode.</summary>
    public const string BackgroundModeNotSupported = "BACKGROUND_MODE_NOT_SUPPORTED";
}
