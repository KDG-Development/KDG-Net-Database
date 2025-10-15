using KDG.Database.Common;
using KDG.Database.DML;
using Xunit;

namespace KDG.Database.Tests.Integration;

public class DeleteOperationTests : PostgreSQLIntegrationTestBase
{
    private const string TestTable = "delete_test";

    [Fact]
    public async Task Delete_WithValidData_DeletesRecord()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            name TEXT NOT NULL
        ");

        var testId = Guid.NewGuid();

        // Insert record first
        await Database.WithTransaction(async transaction =>
        {
            await Database.Insert(transaction, new InsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(testId) },
                    { "name", _ => new DbString("Test Record") }
                }
            });
            return true;
        });

        Assert.Equal(1, await GetRowCount(TestTable));

        var config = new DeleteConfig<object>
        {
            Table = TestTable,
            Data = new { },
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", _ => new DbGuid(testId) }
            }
        };

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.Delete(transaction, config);
            return true;
        });

        // Assert
        Assert.Equal(0, await GetRowCount(TestTable));
    }

    [Fact]
    public async Task Delete_WithDbGuid_WorksCorrectly()
    {
        // Arrange - This test specifically verifies the DbGuid fix
        await CreateTestTable(TestTable, "id UUID PRIMARY KEY, value TEXT");

        var testId1 = Guid.NewGuid();
        var testId2 = Guid.NewGuid();

        // Insert two records
        await Database.WithTransaction(async transaction =>
        {
            await Database.Insert(transaction, new InsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(testId1) },
                    { "value", _ => new DbString("first") }
                }
            });

            await Database.Insert(transaction, new InsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(testId2) },
                    { "value", _ => new DbString("second") }
                }
            });

            return true;
        });

        Assert.Equal(2, await GetRowCount(TestTable));

        // Act - Delete only the first record using DbGuid
        await Database.WithTransaction(async transaction =>
        {
            await Database.Delete(transaction, new DeleteConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(testId1) }
                }
            });
            return true;
        });

        // Assert
        Assert.Equal(1, await GetRowCount(TestTable));
        Assert.False(await RowExists(TestTable, $"id = '{testId1}'"));
        Assert.True(await RowExists(TestTable, $"id = '{testId2}'"));
    }

    [Fact]
    public async Task Delete_WithMultipleConditions_DeletesCorrectRecord()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            category TEXT NOT NULL,
            status TEXT NOT NULL
        ");

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        await Database.WithTransaction(async transaction =>
        {
            await Database.Insert(transaction, new InsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id1) },
                    { "category", _ => new DbString("A") },
                    { "status", _ => new DbString("active") }
                }
            });

            await Database.Insert(transaction, new InsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id2) },
                    { "category", _ => new DbString("A") },
                    { "status", _ => new DbString("inactive") }
                }
            });

            return true;
        });

        // Act - Delete record with specific category AND status
        await Database.WithTransaction(async transaction =>
        {
            await Database.Delete(transaction, new DeleteConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "category", _ => new DbString("A") },
                    { "status", _ => new DbString("active") }
                }
            });
            return true;
        });

        // Assert - Only the active record should be deleted
        Assert.Equal(1, await GetRowCount(TestTable));
        Assert.False(await RowExists(TestTable, $"id = '{id1}'"));
        Assert.True(await RowExists(TestTable, $"id = '{id2}'"));
    }

    [Fact]
    public async Task Delete_NonExistentRecord_DoesNotThrowError()
    {
        // Arrange
        await CreateTestTable(TestTable, "id UUID PRIMARY KEY");

        var nonExistentId = Guid.NewGuid();

        // Act & Assert - Should not throw
        await Database.WithTransaction(async transaction =>
        {
            await Database.Delete(transaction, new DeleteConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(nonExistentId) }
                }
            });
            return true;
        });

        Assert.Equal(0, await GetRowCount(TestTable));
    }
}
