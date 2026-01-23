using FlaUI.Core.AutomationElements;
using WpfMcp.Server.Models;

namespace WpfMcp.Server.Services;

/// <summary>
/// Manages element references within a snapshot context.
/// </summary>
public interface IElementReferenceManager
{
    /// <summary>
    /// Gets the current snapshot ID.
    /// </summary>
    string? CurrentSnapshotId { get; }

    /// <summary>
    /// Creates a new snapshot context, invalidating all previous references.
    /// </summary>
    /// <returns>The new snapshot ID.</returns>
    string BeginNewSnapshot();

    /// <summary>
    /// Registers an element and returns its reference.
    /// </summary>
    /// <param name="element">The automation element to register.</param>
    /// <returns>The element reference.</returns>
    ElementReference RegisterElement(AutomationElement element);

    /// <summary>
    /// Gets the automation element for a reference.
    /// </summary>
    /// <param name="refId">The reference ID (e.g., "e1").</param>
    /// <returns>The automation element, or null if not found.</returns>
    AutomationElement? GetElement(string refId);

    /// <summary>
    /// Gets the element reference for a reference ID.
    /// </summary>
    /// <param name="refId">The reference ID.</param>
    /// <returns>The element reference, or null if not found.</returns>
    ElementReference? GetReference(string refId);

    /// <summary>
    /// Validates if a reference is still valid.
    /// </summary>
    /// <param name="refId">The reference ID to validate.</param>
    /// <returns>True if the reference is valid, false otherwise.</returns>
    bool IsReferenceValid(string refId);

    /// <summary>
    /// Gets the total count of registered elements.
    /// </summary>
    int ElementCount { get; }

    /// <summary>
    /// Clears all references.
    /// </summary>
    void Clear();
}
