# Contributing to WPF-MCP

Thank you for your interest in contributing to WPF-MCP! This document provides guidelines and instructions for contributing.

## Getting Started

### Prerequisites

- Windows 10/11
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension
- A WPF application for testing (Calculator works great)

### Development Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/ricfanin/wpf_mcp.git
   cd wpf_mcp
   ```

2. **Build the project**
   ```bash
   dotnet build
   ```

3. **Run tests**
   ```bash
   dotnet test
   ```

4. **Run in development mode**
   ```bash
   .\scripts\dev.ps1
   ```

## How to Contribute

### Reporting Bugs

1. Check if the bug has already been reported in [Issues](https://github.com/ricfanin/wpf_mcp/issues)
2. If not, create a new issue with:
   - Clear, descriptive title
   - Steps to reproduce
   - Expected vs actual behavior
   - Environment details (OS version, .NET version)
   - Target application details if relevant

### Suggesting Features

1. Open an issue with the `enhancement` label
2. Describe the feature and its use case
3. Explain why it would benefit the project

### Pull Requests

1. **Fork the repository**

2. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make your changes**
   - Follow the existing code style
   - Add tests for new functionality
   - Update documentation as needed

4. **Run quality checks**
   ```bash
   dotnet build
   dotnet test
   ```

5. **Commit with clear messages**
   ```bash
   git commit -m "feat: add drag and drop support"
   ```

6. **Push and create a PR**
   ```bash
   git push origin feature/your-feature-name
   ```

## Code Style

### General Guidelines

- Use meaningful variable and method names
- Keep methods focused and small
- Add XML documentation for public APIs
- Follow existing patterns in the codebase

### Naming Conventions

- **Classes**: PascalCase (`ApplicationManager`)
- **Methods**: PascalCase (`GetSnapshot`)
- **Variables**: camelCase (`elementRef`)
- **Constants**: PascalCase (`ErrorCodes.ElementNotFound`)
- **Interfaces**: IPascalCase (`IApplicationManager`)

### Tool Implementation

When adding new MCP tools:

1. Add to the appropriate `Tools/*Tools.cs` file
2. Use `ToolResponse<T>` for consistent responses
3. Include `element` (human-readable) and `ref` parameters
4. Add XML documentation with examples
5. Handle errors with appropriate `ErrorCodes`
6. Add unit tests

### Example Tool Structure

```csharp
[McpServerTool]
[Description("Brief description of what the tool does")]
public ToolResponse<ResultType> wpf_tool_name(
    [Description("Human-readable element description")]
    string element,

    [Description("Element reference from snapshot")]
    string @ref)
{
    try
    {
        // Validate access
        var validationResult = ValidateElementAccess(@ref, out var uiElement);
        if (!validationResult.Success)
            return validationResult.CastError<ResultType>();

        // Perform operation
        // ...

        return ToolResponse<ResultType>.SuccessResponse(result);
    }
    catch (Exception ex)
    {
        return ToolResponse<ResultType>.FailureResponse(
            ErrorCodes.OperationFailed,
            ex.Message);
    }
}
```

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

### Writing Tests

- Use xUnit for test framework
- Use FluentAssertions for assertions
- Use Moq for mocking
- Follow Arrange-Act-Assert pattern

## Documentation

- Update `README.md` for user-facing changes
- Update `docs/TOOLS_REFERENCE.md` for new tools
- Update `docs/ARCHITECTURE.md` for design changes

## Questions?

Feel free to open an issue for any questions about contributing!
