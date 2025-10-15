using KDG.Database.Common;
using KDG.Database.DML;
using Xunit;

namespace KDG.Database.Tests.Integration.SqlServer;

public class UpdateOperationTests : SqlServerIntegrationTestBase
{
    private const string TestTable = "update_test";

    public class TestRecord
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public int Value { get; set; }
    }

    [Fact]
    public async Task Update_WithValidData_UpdatesRecord()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            name NVARCHAR(255) NOT NULL,
            value INT NOT NULL
        ");

        var testId = Guid.NewGuid();

        // Insert initial record
        await Database.WithTransaction(async transaction =>
        {
            await Database.Insert(transaction, new InsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(testId) },
                    { "name", _ => new DbString("Initial Name") },
                    { "value", _ => new DbNumeric(100) }
                }
            });
            return true;
        });

        // Act - Update the record
        var updatedRecord = new TestRecord
        {
            Id = testId,
            Name = "Updated Name",
            Value = 200
        };

        var config = new UpdateConfig<TestRecord>
        {
            Table = TestTable,
            Data = updatedRecord,
            Key = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "id", r => new DbGuid(r.Id) }
            },
            Fields = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "name", r => new DbString(r.Name) },
                { "value", r => new DbNumeric(r.Value) }
            }
        };

        await Database.WithTransaction(async transaction =>
        {
            await Database.Update(transaction, config);
            return true;
        });

        // Assert
        Assert.Equal(1, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND name = 'Updated Name' AND value = 200"));
        Assert.False(await RowExists(TestTable, $"name = 'Initial Name'"));
    }

    [Fact]
    public async Task Update_WithPredicates_OnlyUpdatesMatchingRecord()
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
                    { "name", _ => new DbString("Name1") },
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
                    { "name", _ => new DbString("Name2") },
                    { "value", _ => new DbNumeric(200) }
                }
            });

            return true;
        });

        // Act - Update only the first record using predicates
        var record = new TestRecord
        {
            Id = id1,
            Name = "Name1",
            Value = 150
        };

        var config = new UpdateConfig<TestRecord>
        {
            Table = TestTable,
            Data = record,
            Key = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "id", r => new DbGuid(r.Id) }
            },
            Predicates = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "name", r => new DbString(r.Name) }
            },
            Fields = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "value", r => new DbNumeric(r.Value) }
            }
        };

        await Database.WithTransaction(async transaction =>
        {
            await Database.Update(transaction, config);
            return true;
        });

        // Assert
        Assert.Equal(2, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, $"id = '{id1}' AND value = 150"));
        Assert.True(await RowExists(TestTable, $"id = '{id2}' AND value = 200")); // Unchanged
    }

    [Fact]
    public async Task Update_WithCompositeKey_UpdatesCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER NOT NULL,
            category NVARCHAR(50) NOT NULL,
            value INT NOT NULL,
            PRIMARY KEY (id, category)
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
                    { "category", _ => new DbString("TypeA") },
                    { "value", _ => new DbNumeric(100) }
                }
            });
            return true;
        });

        // Act - Update using composite key
        await Database.WithTransaction(async transaction =>
        {
            await Database.Update(transaction, new UpdateConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Key = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(testId) },
                    { "category", _ => new DbString("TypeA") }
                },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "value", _ => new DbNumeric(200) }
                }
            });
            return true;
        });

        // Assert
        Assert.Equal(1, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND category = 'TypeA' AND value = 200"));
    }
}



