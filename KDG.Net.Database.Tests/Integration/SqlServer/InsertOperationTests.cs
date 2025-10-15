using KDG.Database.Common;
using KDG.Database.DML;
using System.Text.Json;
using Xunit;

namespace KDG.Database.Tests.Integration.SqlServer;

public class InsertOperationTests : SqlServerIntegrationTestBase
{
    private const string TestTable = "insert_test";

    public class TestRecord
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public int Age { get; set; }
        public decimal Salary { get; set; }
    }

    [Fact]
    public async Task Insert_WithValidData_InsertsRecord()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            name NVARCHAR(255) NOT NULL,
            age INT NOT NULL,
            salary DECIMAL(18,2) NOT NULL
        ");

        var testId = Guid.NewGuid();
        var record = new TestRecord
        {
            Id = testId,
            Name = "John Doe",
            Age = 30,
            Salary = 75000.50m
        };

        var config = new InsertConfig<TestRecord>
        {
            Table = TestTable,
            Data = record,
            Fields = new Dictionary<string, Func<TestRecord, ADbValue>>
            {
                { "id", r => new DbGuid(r.Id) },
                { "name", r => new DbString(r.Name) },
                { "age", r => new DbNumeric(r.Age) },
                { "salary", r => new DbNumeric(r.Salary) }
            }
        };

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.Insert(transaction, config);
            return true;
        });

        // Assert
        var rowCount = await GetRowCount(TestTable);
        Assert.Equal(1, rowCount);
        Assert.True(await RowExists(TestTable, $"id = '{testId}'"));
        Assert.True(await RowExists(TestTable, $"name = 'John Doe'"));
    }

    [Fact]
    public async Task Insert_WithDbGuid_WorksCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, "id UNIQUEIDENTIFIER PRIMARY KEY");

        var testId = Guid.NewGuid();
        var record = new { Id = testId };

        var config = new InsertConfig<object>
        {
            Table = TestTable,
            Data = record,
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", r => new DbGuid(testId) }
            }
        };

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.Insert(transaction, config);
            return true;
        });

        // Assert
        Assert.Equal(1, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, $"id = '{testId}'"));
    }

    [Fact]
    public async Task Insert_MultipleRecords_InsertsAll()
    {
        // Arrange
        await CreateTestTable(TestTable, "id UNIQUEIDENTIFIER PRIMARY KEY, name NVARCHAR(255)");

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.Insert(transaction, new InsertConfig<object>
            {
                Table = TestTable,
                Data = new { Id = id1 },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id1) },
                    { "name", _ => new DbString("First") }
                }
            });

            await Database.Insert(transaction, new InsertConfig<object>
            {
                Table = TestTable,
                Data = new { Id = id2 },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(id2) },
                    { "name", _ => new DbString("Second") }
                }
            });

            return true;
        });

        // Assert
        Assert.Equal(2, await GetRowCount(TestTable));
    }

    [Fact]
    public async Task Insert_WithJsonFromObject_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            data NVARCHAR(MAX) NOT NULL
        ");

        var testId = Guid.NewGuid();
        var profile = new
        {
            Name = "John Doe",
            Age = 30,
            Hobbies = new[] { "Reading", "Gaming" }
        };

        var config = new InsertConfig<object>
        {
            Table = TestTable,
            Data = new { Id = testId },
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", _ => new DbGuid(testId) },
                { "data", _ => new DbJson(profile) }
            }
        };

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.Insert(transaction, config);
            return true;
        });

        // Assert
        Assert.Equal(1, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, $"id = '{testId}'"));
    }

    [Fact]
    public async Task Insert_WithDbBool_WorksCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            is_active BIT NOT NULL
        ");

        var testId = Guid.NewGuid();

        var config = new InsertConfig<object>
        {
            Table = TestTable,
            Data = new { Id = testId },
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", _ => new DbGuid(testId) },
                { "is_active", _ => new DbBool(true) }
            }
        };

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.Insert(transaction, config);
            return true;
        });

        // Assert
        Assert.Equal(1, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND is_active = 1"));
    }

    [Fact]
    public async Task Insert_WithTransactionRollback_DoesNotInsert()
    {
        // Arrange
        await CreateTestTable(TestTable, "id UNIQUEIDENTIFIER PRIMARY KEY");

        var testId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await Database.WithTransaction<bool>(async transaction =>
            {
                await Database.Insert(transaction, new InsertConfig<object>
                {
                    Table = TestTable,
                    Data = new { Id = testId },
                    Fields = new Dictionary<string, Func<object, ADbValue>>
                    {
                        { "id", _ => new DbGuid(testId) }
                    }
                });

                // Force an error to trigger rollback
                throw new Exception("Intentional rollback");
#pragma warning disable CS0162 // Unreachable code detected
                return true;
#pragma warning restore CS0162 // Unreachable code detected
            });
        });

        // Assert - record should not exist due to rollback
        Assert.Equal(0, await GetRowCount(TestTable));
    }
}

