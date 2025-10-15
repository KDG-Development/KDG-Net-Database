using KDG.Database.Common;
using KDG.Database.DML;
using Xunit;

namespace KDG.Database.Tests.Integration.PostgreSQL;

public class UpsertOperationTests : PostgreSQLIntegrationTestBase
{
    private const string TestTable = "upsert_test";

    public class TestRecord
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public int Value { get; set; }
    }

    [Fact]
    public async Task Upsert_NewRecord_InsertsRecord()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            name TEXT NOT NULL,
            value INTEGER NOT NULL
        ");

        var testId = Guid.NewGuid();
        var record = new TestRecord
        {
            Id = testId,
            Name = "New Record",
            Value = 100
        };

        var config = new UpsertConfig<TestRecord>
        {
            Table = TestTable,
            Data = record,
            Fields = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "id", r => new DbGuid(r.Id) },
                { "name", r => new DbString(r.Name) },
                { "value", r => new DbNumeric(r.Value) }
            },
            Key = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "id", r => new DbGuid(r.Id) }
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
        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND name = 'New Record' AND value = 100"));
    }

    [Fact]
    public async Task Upsert_ExistingRecord_UpdatesRecord()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            name TEXT NOT NULL,
            value INTEGER NOT NULL
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
                    { "name", _ => new DbString("Original") },
                    { "value", _ => new DbNumeric(50) }
                }
            });
            return true;
        });

        var updatedRecord = new TestRecord
        {
            Id = testId,
            Name = "Updated",
            Value = 200
        };

        var config = new UpsertConfig<TestRecord>
        {
            Table = TestTable,
            Data = updatedRecord,
            Fields = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "id", r => new DbGuid(r.Id) },
                { "name", r => new DbString(r.Name) },
                { "value", r => new DbNumeric(r.Value) }
            },
            Key = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "id", r => new DbGuid(r.Id) }
            }
        };

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.Upsert(transaction, config);
            return true;
        });

        // Assert
        Assert.Equal(1, await GetRowCount(TestTable)); // Still only one record
        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND name = 'Updated' AND value = 200"));
        Assert.False(await RowExists(TestTable, $"name = 'Original'"));
    }

    [Fact]
    public async Task Upsert_MultipleOperations_HandlesInsertAndUpdate()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            name TEXT NOT NULL,
            value INTEGER NOT NULL
        ");

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        // Insert first record
        await Database.WithTransaction(async transaction =>
        {
            await Database.Upsert(transaction, new UpsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id1) },
                    { "name", _ => new DbString("First") },
                    { "value", _ => new DbNumeric(10) }
                },
                Key = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id1) }
                }
            });
            return true;
        });

        // Act - Upsert first (update) and second (insert)
        await Database.WithTransaction(async transaction =>
        {
            await Database.Upsert(transaction, new UpsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id1) },
                    { "name", _ => new DbString("First Updated") },
                    { "value", _ => new DbNumeric(20) }
                },
                Key = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id1) }
                }
            });

            await Database.Upsert(transaction, new UpsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id2) },
                    { "name", _ => new DbString("Second") },
                    { "value", _ => new DbNumeric(30) }
                },
                Key = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id2) }
                }
            });

            return true;
        });

        // Assert
        Assert.Equal(2, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, $"id = '{id1}' AND name = 'First Updated' AND value = 20"));
        Assert.True(await RowExists(TestTable, $"id = '{id2}' AND name = 'Second' AND value = 30"));
    }

    [Fact]
    public async Task Upsert_WithDbGuid_WorksCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, "id UUID PRIMARY KEY, data TEXT");

        var testId = Guid.NewGuid();

        // Act - First upsert (insert)
        await Database.WithTransaction(async transaction =>
        {
            await Database.Upsert(transaction, new UpsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(testId) },
                    { "data", _ => new DbString("initial") }
                },
                Key = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(testId) }
                }
            });
            return true;
        });

        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND data = 'initial'"));

        // Act - Second upsert (update)
        await Database.WithTransaction(async transaction =>
        {
            await Database.Upsert(transaction, new UpsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(testId) },
                    { "data", _ => new DbString("updated") }
                },
                Key = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(testId) }
                }
            });
            return true;
        });

        // Assert
        Assert.Equal(1, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND data = 'updated'"));
    }

    [Fact]
    public async Task Upsert_CompositeKey_WorksCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            tenant_id UUID NOT NULL,
            user_id UUID NOT NULL,
            value INTEGER NOT NULL,
            PRIMARY KEY (tenant_id, user_id)
        ");

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act - Insert
        await Database.WithTransaction(async transaction =>
        {
            await Database.Upsert(transaction, new UpsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "tenant_id", _ => new DbGuid(tenantId) },
                    { "user_id", _ => new DbGuid(userId) },
                    { "value", _ => new DbNumeric(100) }
                },
                Key = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "tenant_id", _ => new DbGuid(tenantId) },
                    { "user_id", _ => new DbGuid(userId) }
                }
            });
            return true;
        });

        Assert.Equal(1, await GetRowCount(TestTable));

        // Act - Update same composite key
        await Database.WithTransaction(async transaction =>
        {
            await Database.Upsert(transaction, new UpsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "tenant_id", _ => new DbGuid(tenantId) },
                    { "user_id", _ => new DbGuid(userId) },
                    { "value", _ => new DbNumeric(200) }
                },
                Key = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "tenant_id", _ => new DbGuid(tenantId) },
                    { "user_id", _ => new DbGuid(userId) }
                }
            });
            return true;
        });

        // Assert
        Assert.Equal(1, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, $"tenant_id = '{tenantId}' AND user_id = '{userId}' AND value = 200"));
    }
}
