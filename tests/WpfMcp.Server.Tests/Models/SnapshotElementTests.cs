using FluentAssertions;
using WpfMcp.Server.Models;
using Xunit;

namespace WpfMcp.Server.Tests.Models;

public class SnapshotElementTests
{
    [Fact]
    public void ToYamlString_SimpleElement_ShouldFormatCorrectly()
    {
        // Arrange
        var element = new SnapshotElement
        {
            Ref = "e1",
            ControlType = "button",
            Name = "Submit",
            Depth = 0
        };

        // Act
        var yaml = element.ToYamlString();

        // Assert
        yaml.Should().Contain("- button \"Submit\" [ref=e1]");
    }

    [Fact]
    public void ToYamlString_WithValue_ShouldIncludeValue()
    {
        // Arrange
        var element = new SnapshotElement
        {
            Ref = "e1",
            ControlType = "textbox",
            Name = "Username",
            Value = "test@example.com",
            Depth = 0
        };

        // Act
        var yaml = element.ToYamlString();

        // Assert
        yaml.Should().Contain("[value=\"test@example.com\"]");
    }

    [Fact]
    public void ToYamlString_WithStates_ShouldIncludeStates()
    {
        // Arrange
        var element = new SnapshotElement
        {
            Ref = "e1",
            ControlType = "checkbox",
            Name = "Remember Me",
            States = new List<string> { "checked", "focused" },
            Depth = 0
        };

        // Act
        var yaml = element.ToYamlString();

        // Assert
        yaml.Should().Contain("[checked]");
        yaml.Should().Contain("[focused]");
    }

    [Fact]
    public void ToYamlString_WithChildren_ShouldIndentChildren()
    {
        // Arrange
        var element = new SnapshotElement
        {
            Ref = "e1",
            ControlType = "window",
            Name = "Main",
            Depth = 0,
            Children = new List<SnapshotElement>
            {
                new SnapshotElement
                {
                    Ref = "e2",
                    ControlType = "button",
                    Name = "OK",
                    Depth = 1
                }
            }
        };

        // Act
        var yaml = element.ToYamlString();

        // Assert
        yaml.Should().Contain("- window \"Main\" [ref=e1]");
        yaml.Should().Contain("  - button \"OK\" [ref=e2]");
    }

    [Fact]
    public void ToYamlString_WithLongValue_ShouldTruncate()
    {
        // Arrange
        var longValue = new string('a', 100);
        var element = new SnapshotElement
        {
            Ref = "e1",
            ControlType = "textbox",
            Value = longValue,
            Depth = 0
        };

        // Act
        var yaml = element.ToYamlString();

        // Assert
        yaml.Should().Contain("...");
        yaml.Length.Should().BeLessThan(200);
    }

    [Fact]
    public void ToYamlString_WithoutName_ShouldOmitName()
    {
        // Arrange
        var element = new SnapshotElement
        {
            Ref = "e1",
            ControlType = "pane",
            Depth = 0
        };

        // Act
        var yaml = element.ToYamlString();

        // Assert
        yaml.Should().Be("- pane [ref=e1]");
    }
}
