using KDG.Database.Common;
using KDG.Database.DML;
using System.Text.Json;
using Xunit;

namespace KDG.Database.Tests.Integration.PostgreSQL;

public class InsertOperationTests : PostgreSQLIntegrationTestBase
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
            id UUID PRIMARY KEY,
            name TEXT NOT NULL,
            age INTEGER NOT NULL,
            salary NUMERIC NOT NULL
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
        await CreateTestTable(TestTable, "id UUID PRIMARY KEY");

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
        await CreateTestTable(TestTable, "id UUID PRIMARY KEY, name TEXT");

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
            id UUID PRIMARY KEY,
            data JSONB NOT NULL
        ");

        var testId = Guid.NewGuid();
        var profile = new
        {
            Name = "John Doe",
            Age = 30,
            Email = "john@example.com"
        };

        var config = new InsertConfig<object>
        {
            Table = TestTable,
            Data = new { },
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
        Assert.True(await RowExists(TestTable, "data->>'Name' = 'John Doe'"));
        Assert.True(await RowExists(TestTable, "data->>'Email' = 'john@example.com'"));
    }

    [Fact]
    public async Task Insert_WithJsonFromString_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            data JSONB NOT NULL
        ");

        var testId = Guid.NewGuid();
        var jsonString = @"{""title"":""Test"",""value"":123,""active"":true}";

        var config = new InsertConfig<object>
        {
            Table = TestTable,
            Data = new { },
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", _ => new DbGuid(testId) },
                { "data", _ => new DbJson(jsonString) }
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
        Assert.True(await RowExists(TestTable, "data->>'title' = 'Test'"));
        Assert.True(await RowExists(TestTable, "(data->>'value')::int = 123"));
    }

    [Fact]
    public async Task Insert_WithComplexNestedJson_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            data JSONB NOT NULL
        ");

        var testId = Guid.NewGuid();
        var complexData = new
        {
            Title = "Test Article",
            Tags = new[] { "tech", "database", "postgresql" },
            Metadata = new Dictionary<string, object>
            {
                { "author", "John Doe" },
                { "views", 1500 },
                { "published", true }
            }
        };

        var config = new InsertConfig<object>
        {
            Table = TestTable,
            Data = new { },
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", _ => new DbGuid(testId) },
                { "data", _ => new DbJson(complexData) }
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
        Assert.True(await RowExists(TestTable, "data->>'Title' = 'Test Article'"));
        Assert.True(await RowExists(TestTable, "data->'Tags' @> '[\"tech\"]'"));
        Assert.True(await RowExists(TestTable, "data->'Metadata'->>'author' = 'John Doe'"));
    }

    [Fact]
    public async Task Insert_WithDbFloat_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            temperature REAL NOT NULL,
            humidity REAL NOT NULL
        ");

        var testId = Guid.NewGuid();

        var config = new InsertConfig<object>
        {
            Table = TestTable,
            Data = new { },
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", _ => new DbGuid(testId) },
                { "temperature", _ => new DbFloat(23.5f) },
                { "humidity", _ => new DbFloat(65.2f) }
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
    public async Task Insert_WithDbInstant_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            created_at TIMESTAMPTZ NOT NULL,
            updated_at TIMESTAMPTZ NOT NULL
        ");

        var testId = Guid.NewGuid();
        var now = NodaTime.SystemClock.Instance.GetCurrentInstant();
        var earlier = now.Minus(NodaTime.Duration.FromHours(1));

        var config = new InsertConfig<object>
        {
            Table = TestTable,
            Data = new { },
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", _ => new DbGuid(testId) },
                { "created_at", _ => new DbInstant(earlier) },
                { "updated_at", _ => new DbInstant(now) }
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
    public async Task Insert_WithDbInt_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            count INTEGER NOT NULL,
            quantity INTEGER NOT NULL
        ");

        var testId = Guid.NewGuid();

        var config = new InsertConfig<object>
        {
            Table = TestTable,
            Data = new { },
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", _ => new DbGuid(testId) },
                { "count", _ => new DbInt(42) },
                { "quantity", _ => new DbInt(100) }
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
        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND count = 42 AND quantity = 100"));
    }
}
