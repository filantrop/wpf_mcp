using FluentAssertions;
using WpfMcp.Server.Models;
using Xunit;

namespace WpfMcp.Server.Tests.Models;

public class ToolResponseTests
{
    [Fact]
    public void Ok_ShouldCreateSuccessResponse()
    {
        // Arrange
        var data = new { message = "test" };

        // Act
        var response = ToolResponse<object>.Ok(data);

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().Be(data);
        response.Error.Should().BeNull();
        response.Metadata.Should().NotBeNull();
    }

    [Fact]
    public void Ok_WithMetadata_ShouldIncludeMetadata()
    {
        // Arrange
        var data = new { message = "test" };
        var metadata = new ResponseMetadata
        {
            ExecutionTimeMs = 100,
            SnapshotValid = true
        };

        // Act
        var response = ToolResponse<object>.Ok(data, metadata);

        // Assert
        response.Metadata.ExecutionTimeMs.Should().Be(100);
        response.Metadata.SnapshotValid.Should().BeTrue();
    }

    [Fact]
    public void Fail_ShouldCreateErrorResponse()
    {
        // Act
        var response = ToolResponse<object>.Fail(
            ErrorCodes.ElementNotFound,
            "Element not found",
            "Call wpf_snapshot to refresh");

        // Assert
        response.Success.Should().BeFalse();
        response.Data.Should().BeNull();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(ErrorCodes.ElementNotFound);
        response.Error.Message.Should().Be("Element not found");
        response.Error.Suggestion.Should().Be("Call wpf_snapshot to refresh");
        response.Error.Recoverable.Should().BeTrue();
    }

    [Fact]
    public void Fail_WithNonRecoverable_ShouldSetRecoverableFalse()
    {
        // Act
        var response = ToolResponse<object>.Fail(
            ErrorCodes.AppCrashed,
            "Application crashed",
            recoverable: false);

        // Assert
        response.Error!.Recoverable.Should().BeFalse();
    }
}
