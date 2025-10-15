# Integration Tests

This folder contains integration tests that verify database operations against a real PostgreSQL database using **Testcontainers**.

## Architecture

### Testcontainers
- **Automatic PostgreSQL provisioning** - Spins up a Docker container automatically
- **No manual setup required** - Tests handle all infrastructure
- **Consistent environment** - Same PostgreSQL version (16-alpine) for all developers
- **Single container shared** - Single container reused across all tests for speed
- **Automatic cleanup** - Container stops when tests complete

### Database Cleanup
- **Table cleanup** - Each test drops its tables after completion
- **Test isolation** - Each test creates fresh tables with `DROP TABLE IF EXISTS`
- **No state leakage** - Clean slate for every test run

## Test Organization

- **InsertOperationTests.cs** - Tests for INSERT operations
- **UpdateOperationTests.cs** - Tests for UPDATE operations with keys and predicates
- **UpsertOperationTests.cs** - Tests for UPSERT (INSERT...ON CONFLICT) operations
- **DeleteOperationTests.cs** - Tests for DELETE operations (including DbGuid fix verification)
- **BulkInsertOperationTests.cs** - Tests for bulk insert operations and performance

## Prerequisites

### Docker Required
Integration tests require Docker to be installed and running:
- **Windows**: [Docker Desktop](https://docs.docker.com/desktop/install/windows-install/)
- **macOS**: [Docker Desktop](https://docs.docker.com/desktop/install/mac-install/)
- **Linux**: [Docker Engine](https://docs.docker.com/engine/install/)

No PostgreSQL installation or configuration needed! ✨

## Running Tests

### All Integration Tests
```bash
dotnet test --filter "FullyQualifiedName~Integration"
```

### Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~Integration.DeleteOperationTests"
```

### Single Test
```bash
dotnet test --filter "FullyQualifiedName~DeleteOperationTests.Delete_WithDbGuid_WorksCorrectly"
```

### First Run
The first test run will:
1. Pull the `postgres:16-alpine` Docker image (if not cached)
2. Start the PostgreSQL container
3. Run all tests
4. Clean up container

Subsequent runs are much faster as the image is cached.

## Test Infrastructure

### PostgreSQLIntegrationTestBase
Base class providing:
- **Automatic container management** - Starts PostgreSQL container once, shared across all tests
- **Database cleanup between tests** - Drops tables after each test
- **Helper methods**:
  - `CreateTestTable(tableName, columns)` - Create test table with DROP IF EXISTS
  - `GetRowCount(tableName)` - Count rows in table
  - `RowExists(tableName, whereClause)` - Check if rows match condition
  - `ExecuteScalar<T>(sql)` - Execute raw SQL for verification

### Test Isolation Strategy
Each test:
1. Inherits from `PostgreSQLIntegrationTestBase` implementing `IAsyncLifetime`
2. `InitializeAsync()` - Ensures container is running (first test starts it)
3. Test executes with fresh tables (DROP TABLE IF EXISTS)
4. `DisposeAsync()` - Drops all tables created during the test
5. Next test starts with clean slate

### Container Lifecycle
```
First Test Starts
├─ Pull postgres:16-alpine image (if needed)
├─ Start PostgreSQL container
└─ Container stays running

Each Test
├─ InitializeAsync() - Verify container ready
├─ Create tables (with DROP IF EXISTS)
├─ Run test operations
└─ DisposeAsync() - Drop all test tables

All Tests Complete
└─ Container automatically stops
```

## Key Test Coverage

### DbGuid Handling
Tests verify that `DbGuid` parameters work correctly across all operations:
- ✅ Insert with DbGuid
- ✅ Update with DbGuid keys
- ✅ Upsert with DbGuid keys
- ✅ **Delete with DbGuid** (verifies the fix for Npgsql parameter issue)
- ✅ BulkInsert with DbGuid

### Transaction Management
All tests verify:
- Operations complete successfully within transactions
- Data integrity is maintained
- Rollback works correctly on errors

### Edge Cases
- Empty collections
- Non-existent records
- Composite keys
- Multiple conditions
- Large datasets (performance tests)

## CI/CD Integration

### GitHub Actions
```yaml
name: Integration Tests

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Run integration tests
        run: dotnet test --filter "FullyQualifiedName~Integration" --logger "trx;LogFileName=test-results.trx"
      
      - name: Publish test results
        if: always()
        uses: dorny/test-reporter@v1
        with:
          name: Integration Tests
          path: '**/test-results.trx'
          reporter: dotnet-trx
```

**Note:** GitHub Actions runners have Docker pre-installed, so no additional setup needed!

### Azure DevOps
```yaml
- task: UseDotNet@2
  inputs:
    version: '8.0.x'

- task: DotNetCoreCLI@2
  displayName: 'Run Integration Tests'
  inputs:
    command: 'test'
    arguments: '--filter "FullyQualifiedName~Integration"'
```

## Performance Characteristics

### Container Startup
- **First run**: ~10-15 seconds (image pull + container start)
- **Cached runs**: ~2-3 seconds (container start only)

### Test Execution
- **Table cleanup**: Fast with DROP TABLE IF EXISTS
- **Bulk insert 10k records**: < 5 seconds
- **Full test suite**: ~30-60 seconds

## Troubleshooting

### "Docker is not available"
- Ensure Docker Desktop is running
- Check `docker ps` works in terminal
- Restart Docker Desktop if needed

### Tests Hang on Startup
- Check Docker has resources available (CPU, memory)
- Verify no port conflicts (PostgreSQL default: 5432)
- Check Docker logs: `docker logs <container-id>`

### Container Cleanup
Testcontainers automatically cleans up, but if needed:
```bash
# Remove all Testcontainers
docker ps -a | grep testcontainers | awk '{print $1}' | xargs docker rm -f

# Clean up orphaned volumes
docker volume prune
```

### Performance Issues
- Increase Docker resource limits (CPU, memory)
- Use SSD for Docker storage
- Consider running tests sequentially: `dotnet test --max-parallel 1`

## Local Development Tips

### View Container Logs
```bash
# Find the Testcontainers PostgreSQL container
docker ps | grep postgres

# View logs
docker logs <container-id>
```

### Connect to Test Database
While tests are running (with a breakpoint), you can connect:
```bash
# Get connection string from test output or code
docker exec -it <container-id> psql -U postgres -d kdg_test
```

### Skip Integration Tests During Development
```bash
# Run only unit tests
dotnet test --filter "FullyQualifiedName~Unit"
```

## Benefits Over Manual Setup

| Aspect | Manual Setup | Testcontainers |
|--------|-------------|----------------|
| Setup Time | ~15 minutes | ~30 seconds (first run) |
| Dependencies | PostgreSQL installed | Just Docker |
| Consistency | Varies by environment | Identical everywhere |
| Cleanup | Manual | Automatic |
| CI/CD | Complex configuration | Works out of the box |
| Isolation | Risk of conflicts | Perfect isolation |

## Learn More

- [Testcontainers Documentation](https://dotnet.testcontainers.org/)
- [Docker Documentation](https://docs.docker.com/)
- [xUnit Documentation](https://xunit.net/)
