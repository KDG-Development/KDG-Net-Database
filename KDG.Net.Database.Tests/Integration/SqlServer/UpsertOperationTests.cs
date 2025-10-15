using KDG.Database.Common;
using KDG.Database.DML;
using Xunit;

namespace KDG.Database.Tests.Integration.SqlServer;

public class UpsertOperationTests : SqlServerIntegrationTestBase
{
    private const string TestTable = "upsert_test";

    public class TestRecord
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public int Value { get; set; }
    }

    [Fact]
    public async Task Upsert_WithNewRecord_InsertsRecord()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            name NVARCHAR(255) NOT NULL,
            value INT NOT NULL
        ");

        var testId = Guid.NewGuid();
        var record = new TestRecord
        {
            Id = testId,
            Name = "Test Record",
            Value = 100
        };

        var config = new UpsertConfig<TestRecord>
        {
            Table = TestTable,
            Data = record,
            Key = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "id", r => new DbGuid(r.Id) }
            },
            Fields = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "id", r => new DbGuid(r.Id) },
                { "name", r => new DbString(r.Name) },
                { "value", r => new DbNumeric(r.Value) }
            }
        };

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.Upsert(transaction, config);
            return true;
        });

        // Assert
        Assert.Equal(1, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND name = 'Test Record' AND value = 100"));
    }

    [Fact]
    public async Task Upsert_WithExistingRecord_UpdatesRecord()
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
                    { "value", _ => new DbNumeric(50) }
                }
            });
            return true;
        });

        // Act - Upsert with same ID but different values
        var updatedRecord = new TestRecord
        {
            Id = testId,
            Name = "Updated Name",
            Value = 200
        };

        var config = new UpsertConfig<TestRecord>
        {
            Table = TestTable,
            Data = updatedRecord,
            Key = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "id", r => new DbGuid(r.Id) }
            },
            Fields = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "id", r => new DbGuid(r.Id) },
                { "name", r => new DbString(r.Name) },
                { "value", r => new DbNumeric(r.Value) }
            }
        };

        await Database.WithTransaction(async transaction =>
        {
            await Database.Upsert(transaction, config);
            return true;
        });

        // Assert
        Assert.Equal(1, await GetRowCount(TestTable)); // Still only one record
        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND name = 'Updated Name' AND value = 200"));
        Assert.False(await RowExists(TestTable, $"name = 'Initial Name'")); // Old value is gone
    }

    [Fact]
    public async Task Upsert_MultipleUpserts_WorksCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            name NVARCHAR(255) NOT NULL,
            value INT NOT NULL
        ");

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        // Act - First upsert (insert)
        await Database.WithTransaction(async transaction =>
        {
            await Database.Upsert(transaction, new UpsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Key = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id1) }
                },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id1) },
                    { "name", _ => new DbString("First") },
                    { "value", _ => new DbNumeric(100) }
                }
            });
            return true;
        });

        // Second upsert (insert)
        await Database.WithTransaction(async transaction =>
        {
            await Database.Upsert(transaction, new UpsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Key = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id2) }
                },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id2) },
                    { "name", _ => new DbString("Second") },
                    { "value", _ => new DbNumeric(200) }
                }
            });
            return true;
        });

        // Third upsert (update first)
        await Database.WithTransaction(async transaction =>
        {
            await Database.Upsert(transaction, new UpsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Key = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id1) }
                },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id1) },
                    { "name", _ => new DbString("First Updated") },
                    { "value", _ => new DbNumeric(150) }
                }
            });
            return true;
        });

        // Assert
        Assert.Equal(2, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, $"id = '{id1}' AND name = 'First Updated' AND value = 150"));
        Assert.True(await RowExists(TestTable, $"id = '{id2}' AND name = 'Second' AND value = 200"));
    }

    [Fact]
    public async Task Upsert_WithCompositeKey_WorksCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER NOT NULL,
            category NVARCHAR(50) NOT NULL,
            value INT NOT NULL,
            PRIMARY KEY (id, category)
        ");

        var testId = Guid.NewGuid();

        // Act - Insert
        await Database.WithTransaction(async transaction =>
        {
            await Database.Upsert(transaction, new UpsertConfig<object>
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
                    { "id", _ => new DbGuid(testId) },
                    { "category", _ => new DbString("TypeA") },
                    { "value", _ => new DbNumeric(100) }
                }
            });
            return true;
        });

        // Update
        await Database.WithTransaction(async transaction =>
        {
            await Database.Upsert(transaction, new UpsertConfig<object>
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
                    { "id", _ => new DbGuid(testId) },
                    { "category", _ => new DbString("TypeA") },
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



