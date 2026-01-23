namespace WpfMcp.Server.Models;

/// <summary>
/// Represents a reference to a UI Automation element in a snapshot.
/// References are valid only within the snapshot context they were created.
/// </summary>
public sealed class ElementReference
{
    /// <summary>
    /// The unique reference identifier (e.g., "e1", "e2", etc.)
    /// </summary>
    public required string Ref { get; init; }

    /// <summary>
    /// The UI Automation runtime ID for this element.
    /// </summary>
    public required int[] RuntimeId { get; init; }

    /// <summary>
    /// The control type of the element.
    /// </summary>
    public required string ControlType { get; init; }

    /// <summary>
    /// The name of the element (from AutomationProperties.Name).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The AutomationId of the element.
    /// </summary>
    public string? AutomationId { get; init; }

    /// <summary>
    /// The bounding rectangle of the element.
    /// </summary>
    public BoundingRect? BoundingRectangle { get; init; }

    /// <summary>
    /// Whether the element is currently enabled.
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Whether the element is currently visible (on screen).
    /// </summary>
    public bool IsOffscreen { get; init; }

    /// <summary>
    /// The timestamp when this reference was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the bounding rectangle of an element.
/// </summary>
public sealed record BoundingRect(double X, double Y, double Width, double Height);
