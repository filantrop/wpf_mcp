using System.Collections.Concurrent;
using FlaUI.Core.AutomationElements;
using WpfMcp.Server.Models;

namespace WpfMcp.Server.Services;

/// <summary>
/// Manages element references within snapshot contexts.
/// </summary>
public sealed class ElementReferenceManager : IElementReferenceManager
{
    private readonly ConcurrentDictionary<string, (ElementReference Reference, AutomationElement Element)> _elements = new();
    private int _nextId;
    private string? _currentSnapshotId;

    public string? CurrentSnapshotId => _currentSnapshotId;

    public int ElementCount => _elements.Count;

    public string BeginNewSnapshot()
    {
        Clear();
        _currentSnapshotId = Guid.NewGuid().ToString("N")[..8];
        _nextId = 1;
        return _currentSnapshotId;
    }

    public ElementReference RegisterElement(AutomationElement element)
    {
        var refId = $"e{_nextId++}";

        var reference = new ElementReference
        {
            Ref = refId,
            RuntimeId = element.Properties.RuntimeId.ValueOrDefault ?? [],
            ControlType = element.Properties.ControlType.ValueOrDefault.ToString().ToLowerInvariant(),
            Name = element.Properties.Name.ValueOrDefault,
            AutomationId = element.Properties.AutomationId.ValueOrDefault,
            BoundingRectangle = GetBoundingRect(element),
            IsEnabled = element.Properties.IsEnabled.ValueOrDefault,
            IsOffscreen = element.Properties.IsOffscreen.ValueOrDefault
        };

        _elements[refId] = (reference, element);

        return reference;
    }

    public AutomationElement? GetElement(string refId)
    {
        return _elements.TryGetValue(refId, out var entry) ? entry.Element : null;
    }

    public ElementReference? GetReference(string refId)
    {
        return _elements.TryGetValue(refId, out var entry) ? entry.Reference : null;
    }

    public bool IsReferenceValid(string refId)
    {
        if (!_elements.TryGetValue(refId, out var entry))
        {
            return false;
        }

        try
        {
            // Try to access the element to check if it's still valid
            _ = entry.Element.Properties.ProcessId.Value;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Clear()
    {
        _elements.Clear();
        _nextId = 1;
    }

    private static BoundingRect? GetBoundingRect(AutomationElement element)
    {
        try
        {
            var rect = element.Properties.BoundingRectangle.ValueOrDefault;
            if (rect.IsEmpty) return null;

            return new BoundingRect(rect.X, rect.Y, rect.Width, rect.Height);
        }
        catch
        {
            return null;
        }
    }
}
