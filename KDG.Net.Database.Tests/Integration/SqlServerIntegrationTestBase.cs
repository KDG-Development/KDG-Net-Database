using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace KDG.Database.Tests.Integration;

/// <summary>
/// Base class for SQL Server integration tests using Testcontainers
/// Provides automatic container lifecycle management and database cleanup between tests
/// </summary>
public abstract class SqlServerIntegrationTestBase : IAsyncLifetime
{
    private static readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Test@123456")
        .Build();

    private static bool _containerInitialized = false;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    protected KDG.Database.SqlServer Database { get; private set; } = null!;
    protected string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Initializes the container and database connection
    /// </summary>
    public async Task InitializeAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (!_containerInitialized)
            {
                await _container.StartAsync();
                _containerInitialized = true;
            }
        }
        finally
        {
            _initLock.Release();
        }

        Database = new KDG.Database.SqlServer(_container.GetConnectionString());
    }

    /// <summary>
    /// Cleanup after test - no action needed since each test drops its own tables
    /// </summary>
    public Task DisposeAsync()
    {
        // No cleanup needed - tests use DROP TABLE IF EXISTS before CREATE TABLE
        // Container cleanup happens automatically when all tests complete
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a test table with the specified columns
    /// Drops the table first if it exists to ensure clean state
    /// </summary>
    protected async Task CreateTestTable(string tableName, string columnDefinitions)
    {
        await Database.WithConnection(async conn =>
        {
            using var cmd = conn.CreateCommand();
            // Drop the table if it exists, then create it fresh
            cmd.CommandText = $@"
                IF OBJECT_ID('{tableName}', 'U') IS NOT NULL
                    DROP TABLE [{tableName}];
                CREATE TABLE [{tableName}] (
                    {columnDefinitions}
                );
            ";
            await cmd.ExecuteNonQueryAsync();
            return true;
        });
    }

    /// <summary>
    /// Gets the count of rows in the specified table
    /// </summary>
    protected async Task<int> GetRowCount(string tableName)
    {
        return await Database.WithConnection(async conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM [{tableName}]";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        });
    }

    /// <summary>
    /// Verifies that a row exists in the table with the given condition
    /// </summary>
    protected async Task<bool> RowExists(string tableName, string whereClause)
    {
        return await Database.WithConnection(async conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM [{tableName}] WHERE {whereClause}";
            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        });
    }

    /// <summary>
    /// Executes a raw SQL query for test setup or verification
    /// </summary>
    protected async Task<T> ExecuteScalar<T>(string sql)
    {
        return await Database.WithConnection(async conn =>
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var result = await cmd.ExecuteScalarAsync();
            return (T)Convert.ChangeType(result!, typeof(T));
        });
    }
}

