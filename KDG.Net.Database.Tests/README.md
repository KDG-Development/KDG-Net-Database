# KDG.Net.Database Tests

Comprehensive test suite for the KDG.Net.Database library with **Unit** and **Integration** tests.

## 📁 Project Structure

```
KDG.Net.Database.Tests/
├── Unit/                          # Fast, isolated tests with mocks
│   ├── DbValue/
│   │   └── DbNullableTests.cs
│   └── README.md
├── Integration/                   # Real database tests
│   ├── PostgreSQLIntegrationTestBase.cs
│   ├── InsertOperationTests.cs
│   ├── UpdateOperationTests.cs
│   ├── UpsertOperationTests.cs
│   ├── DeleteOperationTests.cs
│   ├── BulkInsertOperationTests.cs
│   └── README.md
├── KDG.Net.Database.Tests.csproj
├── env.example
└── README.md (this file)
```

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- Docker (for integration tests only)

### Run All Tests
```bash
dotnet test
```

### Run Unit Tests Only (Fast)
```bash
dotnet test --filter "FullyQualifiedName~Unit"
```

### Run Integration Tests Only (Requires Docker)
```bash
dotnet test --filter "FullyQualifiedName~Integration"
```

## 📊 Test Coverage

### Unit Tests
- ✅ DbNullable value handling
- ✅ Parameter addition with mocked builders
- ✅ Null vs value scenarios

### Integration Tests
All database operations tested with real PostgreSQL:
- ✅ **INSERT** - Single and multiple record insertion
- ✅ **UPDATE** - With keys and predicates
- ✅ **UPSERT** - Insert or update with conflict resolution
- ✅ **DELETE** - Single and multiple conditions (DbGuid fix verification)
- ✅ **BULK INSERT** - Performance testing with large datasets

#### DbGuid Fix Verification
The `DeleteOperationTests.Delete_WithDbGuid_WorksCorrectly` test specifically verifies that the DbGuid parameter handling fix works correctly, addressing the original issue:
> "Writing values of 'KDG.Database.Common.DbGuid' is not supported for parameters having no NpgsqlDbType or DataTypeName"

## 🛠 Technology Stack

### Testing Frameworks
- **xUnit** - Test framework
- **Moq** - Mocking library (unit tests)
- **Testcontainers** - Automatic Docker container management (integration tests)

### Key Features

#### Testcontainers
- 🐳 Automatic PostgreSQL provisioning
- 🔄 Consistent test environment
- 🧹 Automatic cleanup
- ⚡ Single container shared across tests

#### Test Isolation
- 🎯 Perfect test isolation with table cleanup
- 🔄 Fresh tables for each test (DROP IF EXISTS)
- 📋 No state leakage between tests
- ⚡ Fast parallel execution

## 📚 Documentation

- [Unit Tests README](Unit/README.md) - Unit testing guidelines
- [Integration Tests README](Integration/README.md) - Detailed integration test documentation

## 🔧 Configuration

### Integration Tests
No configuration needed! Tests automatically:
1. Start PostgreSQL container (postgres:16-alpine)
2. Run tests with automatic cleanup
3. Stop container when complete

For advanced scenarios, see [Integration Tests README](Integration/README.md).

## 🏃 Running Tests in Different Environments

### Local Development
```bash
# Quick unit tests during development
dotnet test --filter "FullyQualifiedName~Unit"

# Full integration tests before commit
dotnet test --filter "FullyQualifiedName~Integration"
```

### CI/CD Pipeline
```bash
# Both unit and integration tests
dotnet test --logger "trx;LogFileName=test-results.trx"
```

### Specific Test
```bash
dotnet test --filter "FullyQualifiedName~DeleteOperationTests.Delete_WithDbGuid_WorksCorrectly"
```

## 📈 Performance Benchmarks

| Operation | Test Count | Duration | Notes |
|-----------|-----------|----------|-------|
| Unit Tests | ~10 | < 1s | No external dependencies |
| Integration Tests (first run) | ~25 | ~30-45s | Includes container startup |
| Integration Tests (cached) | ~25 | ~15-30s | Container already pulled |
| Bulk Insert (10k records) | 1 | < 5s | Performance baseline |

## 🐛 Troubleshooting

### Docker Not Found
**Symptom**: Tests fail with "Docker is not available"
**Solution**: Ensure Docker Desktop is running

### Tests Hang
**Symptom**: Tests appear to hang during startup
**Solution**: 
1. First run downloads postgres image (~30MB) - be patient
2. Check Docker has resources available
3. Run `docker ps` to verify containers can start

### Build Errors
**Symptom**: Compilation errors
**Solution**: 
```bash
dotnet clean
dotnet restore
dotnet build
```

## 📝 Adding New Tests

### Unit Test
1. Create file in `Unit/<Category>/`
2. Use namespace: `KDG.Database.Tests.Unit.<Category>`
3. Follow AAA pattern (Arrange, Act, Assert)

Example:
```csharp
namespace KDG.Database.Tests.Unit.MyFeature;

public class MyTests
{
    [Fact]
    public void Method_Scenario_ExpectedResult()
    {
        // Arrange
        var mock = new Mock<IDependency>();
        
        // Act
        var result = subject.Method();
        
        // Assert
        Assert.Equal(expected, result);
    }
}
```

### Integration Test
1. Create file in `Integration/`
2. Inherit from `PostgreSQLIntegrationTestBase`
3. Use unique table names per test class

Example:
```csharp
namespace KDG.Database.Tests.Integration;

public class MyOperationTests : PostgreSQLIntegrationTestBase
{
    private const string TestTable = "my_operation_test";
    
    [Fact]
    public async Task Operation_Scenario_ExpectedResult()
    {
        // Arrange
        await CreateTestTable(TestTable, "id UUID PRIMARY KEY");
        
        // Act
        await Database.WithTransaction(async transaction =>
        {
            // Your database operation
            return true;
        });
        
        // Assert
        Assert.Equal(1, await GetRowCount(TestTable));
    }
}
```

## 🎯 Best Practices

### Unit Tests
- ✅ Mock external dependencies
- ✅ Test single units of functionality
- ✅ Keep tests fast (< 100ms each)
- ✅ No database or network access

### Integration Tests
- ✅ Test real database operations
- ✅ Use unique table names per test class
- ✅ Verify actual data in database
- ✅ Test edge cases and error scenarios
- ✅ Include performance tests for bulk operations

## 📄 License

See [LICENSE](../KDG.Net.Database/LICENSE) in the main project.

