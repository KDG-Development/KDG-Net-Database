# Unit Tests

This folder contains unit tests that verify component behavior in isolation using mocks.

## Test Organization

- **DbValue/** - Tests for database value type wrappers
  - `DbNullableTests.cs` - Tests for nullable value handling

## Running Tests

### All Unit Tests
```bash
dotnet test --filter "FullyQualifiedName~Unit"
```

### Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~DbNullableTests"
```

## Unit vs Integration Tests

### Unit Tests (This Folder)
- Use mocks and stubs
- No database connection required
- Fast execution
- Test individual components in isolation
- Run in any environment

### Integration Tests (../Integration)
- Use real database connections
- Verify end-to-end behavior
- Test interactions between components
- Require PostgreSQL database

## Adding New Unit Tests

1. Create test file in appropriate subfolder
2. Use namespace pattern: `KDG.Database.Tests.Unit.<Category>`
3. Use Moq for mocking dependencies
4. Follow AAA pattern (Arrange, Act, Assert)
5. Name tests descriptively: `MethodName_Scenario_ExpectedOutcome`

## Example Test Structure

```csharp
using Moq;
using Xunit;

namespace KDG.Database.Tests.Unit.MyFeature;

public class MyComponentTests
{
    [Fact]
    public void Method_WithValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var mockDependency = new Mock<IDependency>();
        mockDependency.Setup(d => d.DoSomething()).Returns(42);
        var component = new MyComponent(mockDependency.Object);

        // Act
        var result = component.Method("input");

        // Assert
        Assert.Equal(expected, result);
        mockDependency.Verify(d => d.DoSomething(), Times.Once);
    }
}
```

