using KDG.Database.Common;
using KDG.Database.DML;
using Xunit;

namespace KDG.Database.Tests.Integration.SqlServer;

public class DeleteOperationTests : SqlServerIntegrationTestBase
{
    private const string TestTable = "delete_test";

    [Fact]
    public async Task Delete_WithSingleCondition_DeletesRecord()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            name NVARCHAR(255) NOT NULL
        ");

        var testId = Guid.NewGuid();

        // Insert a record
        await Database.WithTransaction(async transaction =>
        {
            await Database.Insert(transaction, new InsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(testId) },
                    { "name", _ => new DbString("Test Name") }
                }
            });
            return true;
        });

        // Act - Delete the record
        await Database.WithTransaction(async transaction =>
        {
            await Database.Delete(transaction, new DeleteConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(testId) }
                }
            });
            return true;
        });

        // Assert
        Assert.Equal(0, await GetRowCount(TestTable));
        Assert.False(await RowExists(TestTable, $"id = '{testId}'"));
    }

    [Fact]
    public async Task Delete_WithMultipleConditions_DeletesOnlyMatchingRecords()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            name NVARCHAR(255) NOT NULL,
            value INT NOT NULL
        ");

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        // Insert two records
        await Database.WithTransaction(async transaction =>
        {
            await Database.Insert(transaction, new InsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id1) },
                    { "name", _ => new DbString("Keep") },
                    { "value", _ => new DbNumeric(100) }
                }
            });

            await Database.Insert(transaction, new InsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id2) },
                    { "name", _ => new DbString("Delete") },
                    { "value", _ => new DbNumeric(200) }
                }
            });

            return true;
        });

        // Act - Delete only the second record
        await Database.WithTransaction(async transaction =>
        {
            await Database.Delete(transaction, new DeleteConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id2) },
                    { "name", _ => new DbString("Delete") }
                }
            });
            return true;
        });

        // Assert
        Assert.Equal(1, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, $"id = '{id1}'"));
        Assert.False(await RowExists(TestTable, $"id = '{id2}'"));
    }

    [Fact]
    public async Task Delete_WithTransactionRollback_DoesNotDelete()
    {
        // Arrange
        await CreateTestTable(TestTable, "id UNIQUEIDENTIFIER PRIMARY KEY");

        var testId = Guid.NewGuid();

        // Insert a record
        await Database.WithTransaction(async transaction =>
        {
            await Database.Insert(transaction, new InsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(testId) }
                }
            });
            return true;
        });

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await Database.WithTransaction<bool>(async transaction =>
            {
                await Database.Delete(transaction, new DeleteConfig<object>
                {
                    Table = TestTable,
                    Data = new { },
                    Fields = new Dictionary<string, Func<object, ADbValue>>
                    {
                        { "id", _ => new DbGuid(testId) }
                    }
                });

                // Force rollback
                throw new Exception("Intentional rollback");
#pragma warning disable CS0162 // Unreachable code detected
                return true;
#pragma warning restore CS0162 // Unreachable code detected
            });
        });

        // Assert - record should still exist due to rollback
        Assert.Equal(1, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, $"id = '{testId}'"));
    }
}

