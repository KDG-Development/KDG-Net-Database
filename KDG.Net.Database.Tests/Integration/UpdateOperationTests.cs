using KDG.Database.Common;
using KDG.Database.DML;
using System.Text.Json;
using Xunit;

namespace KDG.Database.Tests.Integration;

public class UpdateOperationTests : PostgreSQLIntegrationTestBase
{
    private const string TestTable = "update_test";

    public class TestRecord
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public int Version { get; set; }
    }

    [Fact]
    public async Task Update_WithValidData_UpdatesRecord()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            name TEXT NOT NULL,
            version INTEGER NOT NULL
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
                    { "name", _ => new DbString("Original Name") },
                    { "version", _ => new DbNumeric(1) }
                }
            });
            return true;
        });

        var updatedRecord = new TestRecord
        {
            Id = testId,
            Name = "Updated Name",
            Version = 2
        };

        var config = new UpdateConfig<TestRecord>
        {
            Table = TestTable,
            Data = updatedRecord,
            Fields = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "name", r => new DbString(r.Name) },
                { "version", r => new DbNumeric(r.Version) }
            },
            Key = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "id", r => new DbGuid(r.Id) }
            }
        };

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.Update(transaction, config);
            return true;
        });

        // Assert
        Assert.Equal(1, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, $"name = 'Updated Name' AND version = 2"));
    }

    [Fact]
    public async Task Update_WithPredicates_UpdatesOnlyMatchingRecords()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            name TEXT NOT NULL,
            version INTEGER NOT NULL
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
                    { "version", _ => new DbNumeric(1) }
                }
            });
            return true;
        });

        var updatedRecord = new TestRecord
        {
            Id = testId,
            Name = "Updated",
            Version = 2
        };

        var config = new UpdateConfig<TestRecord>
        {
            Table = TestTable,
            Data = updatedRecord,
            Fields = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "name", r => new DbString(r.Name) },
                { "version", r => new DbNumeric(r.Version) }
            },
            Key = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "id", r => new DbGuid(r.Id) }
            },
            Predicates = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "version", _ => new DbNumeric(1) } // Only update if version is 1
            }
        };

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.Update(transaction, config);
            return true;
        });

        // Assert
        Assert.True(await RowExists(TestTable, $"name = 'Updated' AND version = 2"));
    }

    [Fact]
    public async Task Update_WithDbGuid_WorksCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, "id UUID PRIMARY KEY, value TEXT");

        var testId = Guid.NewGuid();

        await Database.WithTransaction(async transaction =>
        {
            await Database.Insert(transaction, new InsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(testId) },
                    { "value", _ => new DbString("original") }
                }
            });
            return true;
        });

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.Update(transaction, new UpdateConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "value", _ => new DbString("updated") }
                },
                Key = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(testId) }
                }
            });
            return true;
        });

        // Assert
        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND value = 'updated'"));
    }

    [Fact]
    public async Task Update_WithJsonData_UpdatesCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            data JSONB NOT NULL
        ");

        var testId = Guid.NewGuid();
        
        // Insert initial data
        await Database.WithTransaction(async transaction =>
        {
            await Database.Insert(transaction, new InsertConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(testId) },
                    { "data", _ => new DbJson(new { Name = "Old Name", Status = "inactive" }) }
                }
            });
            return true;
        });

        // Act - Update with new JSON
        var updatedData = new { Name = "Updated Name", Status = "active" };
        
        await Database.WithTransaction(async transaction =>
        {
            await Database.Update(transaction, new UpdateConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "data", _ => new DbJson(updatedData) }
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
        Assert.True(await RowExists(TestTable, "data->>'Name' = 'Updated Name'"));
        Assert.True(await RowExists(TestTable, "data->>'Status' = 'active'"));
    }
}
