using FluentAssertions;
using Moq;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Identifiers;
using WpfMcp.Server.Services;
using Xunit;

namespace WpfMcp.Server.Tests.Services;

public class ElementReferenceManagerTests
{
    private readonly ElementReferenceManager _sut;

    public ElementReferenceManagerTests()
    {
        _sut = new ElementReferenceManager();
    }

    [Fact]
    public void BeginNewSnapshot_ShouldResetElementCountAndGenerateNewId()
    {
        // Act
        var snapshotId1 = _sut.BeginNewSnapshot();
        var snapshotId2 = _sut.BeginNewSnapshot();

        // Assert
        snapshotId1.Should().NotBeNullOrEmpty();
        snapshotId2.Should().NotBeNullOrEmpty();
        snapshotId1.Should().NotBe(snapshotId2);
        _sut.ElementCount.Should().Be(0);
    }

    [Fact]
    public void CurrentSnapshotId_ShouldReturnNullBeforeFirstSnapshot()
    {
        // Assert
        _sut.CurrentSnapshotId.Should().BeNull();
    }

    [Fact]
    public void CurrentSnapshotId_ShouldReturnIdAfterBeginNewSnapshot()
    {
        // Act
        var snapshotId = _sut.BeginNewSnapshot();

        // Assert
        _sut.CurrentSnapshotId.Should().Be(snapshotId);
    }

    [Fact]
    public void Clear_ShouldResetElementCount()
    {
        // Arrange
        _sut.BeginNewSnapshot();

        // Act
        _sut.Clear();

        // Assert
        _sut.ElementCount.Should().Be(0);
    }

    [Fact]
    public void GetElement_WithInvalidRef_ShouldReturnNull()
    {
        // Arrange
        _sut.BeginNewSnapshot();

        // Act
        var result = _sut.GetElement("invalid_ref");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetReference_WithInvalidRef_ShouldReturnNull()
    {
        // Arrange
        _sut.BeginNewSnapshot();

        // Act
        var result = _sut.GetReference("invalid_ref");

        // Assert
        result.Should().BeNull();
    }
}
