using System.Text.Json.Serialization;

namespace WpfMcp.Server.Models;

/// <summary>
/// Represents an element in the accessibility tree snapshot.
/// </summary>
public sealed class SnapshotElement
{
    /// <summary>
    /// The element reference ID (e.g., "e1").
    /// </summary>
    [JsonPropertyName("ref")]
    public required string Ref { get; init; }

    /// <summary>
    /// The control type (e.g., "button", "textbox").
    /// </summary>
    [JsonPropertyName("controlType")]
    public required string ControlType { get; init; }

    /// <summary>
    /// The element name from AutomationProperties.Name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// The AutomationId of the element.
    /// </summary>
    [JsonPropertyName("automationId")]
    public string? AutomationId { get; init; }

    /// <summary>
    /// Current value for value-supporting elements.
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; init; }

    /// <summary>
    /// Element states (enabled, focused, checked, etc.).
    /// </summary>
    [JsonPropertyName("states")]
    public List<string> States { get; init; } = [];

    /// <summary>
    /// Child elements in the tree.
    /// </summary>
    [JsonPropertyName("children")]
    public List<SnapshotElement> Children { get; init; } = [];

    /// <summary>
    /// Depth in the tree (0 = root).
    /// </summary>
    [JsonPropertyName("depth")]
    public int Depth { get; init; }

    /// <summary>
    /// Formats this element as a YAML-like string for LLM readability.
    /// </summary>
    public string ToYamlString(int indentLevel = 0)
    {
        var indent = new string(' ', indentLevel * 2);
        var parts = new List<string>();

        // Build the main line: - controlType "name" [ref=eX] [states]
        var line = $"{indent}- {ControlType}";

        if (!string.IsNullOrEmpty(Name))
        {
            line += $" \"{Name}\"";
        }

        line += $" [ref={Ref}]";

        // Add value if present
        if (!string.IsNullOrEmpty(Value))
        {
            line += $" [value=\"{TruncateValue(Value)}\"]";
        }

        // Add states
        foreach (var state in States)
        {
            line += $" [{state}]";
        }

        parts.Add(line);

        // Add children recursively
        foreach (var child in Children)
        {
            parts.Add(child.ToYamlString(indentLevel + 1));
        }

        return string.Join(Environment.NewLine, parts);
    }

    private static string TruncateValue(string value, int maxLength = 50)
    {
        if (value.Length <= maxLength) return value;
        return value[..(maxLength - 3)] + "...";
    }
}

/// <summary>
/// Result of a snapshot operation.
/// </summary>
public sealed class SnapshotResult
{
    /// <summary>
    /// The root element of the snapshot tree.
    /// </summary>
    [JsonPropertyName("tree")]
    public required SnapshotElement Tree { get; init; }

    /// <summary>
    /// Total count of elements in the snapshot.
    /// </summary>
    [JsonPropertyName("elementCount")]
    public int ElementCount { get; init; }

    /// <summary>
    /// YAML-formatted string representation for LLM consumption.
    /// </summary>
    [JsonPropertyName("yaml")]
    public string Yaml => Tree.ToYamlString();
}
