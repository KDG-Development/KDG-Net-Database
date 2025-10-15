using KDG.Database.Common;
using KDG.Database.DML;
using System.Text.Json;
using Xunit;

namespace KDG.Database.Tests.Integration.PostgreSQL;

public class BulkInsertOperationTests : PostgreSQLIntegrationTestBase
{
    private const string TestTable = "bulk_insert_test";

    public class TestRecord
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public int Value { get; set; }
    }

    [Fact]
    public async Task BulkInsert_WithMultipleRecords_InsertsAll()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            name TEXT NOT NULL,
            value NUMERIC NOT NULL
        ");

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        var records = new List<TestRecord>
        {
            new TestRecord { Id = id1, Name = "Record 1", Value = 100 },
            new TestRecord { Id = id2, Name = "Record 2", Value = 200 },
            new TestRecord { Id = id3, Name = "Record 3", Value = 300 }
        };

        var config = new BulkInsertConfig<TestRecord>
        {
            Table = TestTable,
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
            await Database.BulkInsert(transaction, records, config);
            return true;
        });

        // Assert
        Assert.Equal(3, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, $"id = '{id1}'"));
        Assert.True(await RowExists(TestTable, $"id = '{id2}'"));
        Assert.True(await RowExists(TestTable, $"id = '{id3}'"));
    }

    public class LargeDatasetRecord
    {
        public Guid Id { get; set; }
        public int Sequence { get; set; }
        public string Data { get; set; } = null!;
    }

    [Fact]
    public async Task BulkInsert_WithLargeDataset_InsertsAllRecords()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            sequence NUMERIC NOT NULL,
            data TEXT NOT NULL
        ");

        var recordCount = 1000;
        var records = Enumerable.Range(1, recordCount)
            .Select(i => new LargeDatasetRecord
            {
                Id = Guid.NewGuid(),
                Sequence = i,
                Data = $"Data for record {i}"
            })
            .ToList();

        var config = new BulkInsertConfig<LargeDatasetRecord>
        {
            Table = TestTable,
            Fields = new Dictionary<string, Func<LargeDatasetRecord, ADbValue>>
            {
                { "id", r => new DbGuid(r.Id) },
                { "sequence", r => new DbNumeric(r.Sequence) },
                { "data", r => new DbString(r.Data) }
            }
        };

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.BulkInsert(transaction, records, config);
            return true;
        });

        // Assert
        Assert.Equal(recordCount, await GetRowCount(TestTable));
    }

    [Fact]
    public async Task BulkInsert_WithDbGuid_WorksCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, "id UUID PRIMARY KEY, label TEXT");

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        var records = new[]
        {
            new { Id = id1, Label = "First" },
            new { Id = id2, Label = "Second" },
            new { Id = id3, Label = "Third" }
        };

        var config = new BulkInsertConfig<object>
        {
            Table = TestTable,
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", r => new DbGuid(((dynamic)r).Id) },
                { "label", r => new DbString(((dynamic)r).Label) }
            }
        };

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.BulkInsert(transaction, records, config);
            return true;
        });

        // Assert
        Assert.Equal(3, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, $"id = '{id1}'"));
        Assert.True(await RowExists(TestTable, $"id = '{id2}'"));
        Assert.True(await RowExists(TestTable, $"id = '{id3}'"));
    }

    [Fact]
    public async Task BulkInsert_EmptyCollection_DoesNotThrow()
    {
        // Arrange
        await CreateTestTable(TestTable, "id UUID PRIMARY KEY");

        var records = new List<object>();

        var config = new BulkInsertConfig<object>
        {
            Table = TestTable,
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", r => new DbGuid(Guid.NewGuid()) }
            }
        };

        // Act & Assert - Should not throw
        await Database.WithTransaction(async transaction =>
        {
            await Database.BulkInsert(transaction, records, config);
            return true;
        });

        Assert.Equal(0, await GetRowCount(TestTable));
    }

    public class VariousTypesRecord
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = null!;
        public int Int { get; set; }
        public decimal Decimal { get; set; }
        public bool Bool { get; set; }
        public NodaTime.LocalDate Date { get; set; }
    }

    [Fact]
    public async Task BulkInsert_WithVariousDbValueTypes_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            text_col TEXT,
            int_col NUMERIC,
            decimal_col NUMERIC,
            bool_col BOOLEAN,
            date_col DATE
        ");

        var records = new[]
        {
            new VariousTypesRecord
            {
                Id = Guid.NewGuid(),
                Text = "Sample text",
                Int = 42,
                Decimal = 123.45m,
                Bool = true,
                Date = NodaTime.LocalDate.FromDateTime(DateTime.Now)
            }
        };

        var config = new BulkInsertConfig<VariousTypesRecord>
        {
            Table = TestTable,
            Fields = new Dictionary<string, Func<VariousTypesRecord, ADbValue>>
            {
                { "id", r => new DbGuid(r.Id) },
                { "text_col", r => new DbString(r.Text) },
                { "int_col", r => new DbNumeric(r.Int) },
                { "decimal_col", r => new DbNumeric(r.Decimal) },
                { "bool_col", r => new DbBool(r.Bool) },
                { "date_col", r => new DbDate(r.Date) }
            }
        };

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.BulkInsert(transaction, records, config);
            return true;
        });

        // Assert
        Assert.Equal(1, await GetRowCount(TestTable));
    }

    [Fact]
    public async Task BulkInsert_PerformanceTest_CompletesInReasonableTime()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            data TEXT NOT NULL,
            value NUMERIC NOT NULL
        ");

        var recordCount = 10000;
        var records = Enumerable.Range(1, recordCount)
            .Select(i => new
            {
                Id = Guid.NewGuid(),
                Data = $"Performance test data {i}",
                Value = i * 1.5m
            })
            .ToList();

        var config = new BulkInsertConfig<object>
        {
            Table = TestTable,
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", r => new DbGuid(((dynamic)r).Id) },
                { "data", r => new DbString(((dynamic)r).Data) },
                { "value", r => new DbNumeric(((dynamic)r).Value) }
            }
        };

        var startTime = DateTime.UtcNow;

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.BulkInsert(transaction, records, config);
            return true;
        });

        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.Equal(recordCount, await GetRowCount(TestTable));
        // Bulk insert should be fast - 10k records in under 5 seconds
        Assert.True(duration.TotalSeconds < 5, $"Bulk insert took {duration.TotalSeconds} seconds, expected < 5");
    }

    [Fact]
    public async Task BulkInsert_WithJsonData_InsertsAllRecords()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            data JSONB NOT NULL
        ");

        var records = new[]
        {
            new { Id = Guid.NewGuid(), Profile = new { Name = "User1", Age = 25, Email = "user1@test.com" } },
            new { Id = Guid.NewGuid(), Profile = new { Name = "User2", Age = 30, Email = "user2@test.com" } },
            new { Id = Guid.NewGuid(), Profile = new { Name = "User3", Age = 35, Email = "user3@test.com" } }
        };

        var config = new BulkInsertConfig<object>
        {
            Table = TestTable,
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", r => new DbGuid(((dynamic)r).Id) },
                { "data", r => new DbJson(((dynamic)r).Profile) }
            }
        };

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.BulkInsert(transaction, records, config);
            return true;
        });

        // Assert
        Assert.Equal(3, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, "data->>'Name' = 'User1'"));
        Assert.True(await RowExists(TestTable, "data->>'Name' = 'User2'"));
        Assert.True(await RowExists(TestTable, "data->>'Name' = 'User3'"));
    }

    [Fact]
    public async Task BulkInsert_WithComplexJsonStructures_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            data JSONB NOT NULL
        ");

        var records = new[]
        {
            new {
                Id = Guid.NewGuid(),
                Data = new {
                    Title = "Article 1",
                    Tags = new[] { "tech", "ai" },
                    Metadata = new Dictionary<string, object> { { "views", 100 } }
                }
            },
            new {
                Id = Guid.NewGuid(),
                Data = new {
                    Title = "Article 2",
                    Tags = new[] { "database", "sql" },
                    Metadata = new Dictionary<string, object> { { "views", 250 } }
                }
            }
        };

        var config = new BulkInsertConfig<object>
        {
            Table = TestTable,
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", r => new DbGuid(((dynamic)r).Id) },
                { "data", r => new DbJson(((dynamic)r).Data) }
            }
        };

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.BulkInsert(transaction, records, config);
            return true;
        });

        // Assert
        Assert.Equal(2, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, "data->>'Title' = 'Article 1'"));
        Assert.True(await RowExists(TestTable, "data->'Tags' @> '[\"tech\"]'"));
    }

    [Fact]
    public async Task BulkInsert_WithDbFloat_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            measurement REAL NOT NULL,
            score REAL NOT NULL
        ");

        var records = new[]
        {
            new { Id = Guid.NewGuid(), Measurement = 12.34f, Score = 98.7f },
            new { Id = Guid.NewGuid(), Measurement = 56.78f, Score = 87.6f },
            new { Id = Guid.NewGuid(), Measurement = 90.12f, Score = 76.5f }
        };

        var config = new BulkInsertConfig<object>
        {
            Table = TestTable,
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", r => new DbGuid(((dynamic)r).Id) },
                { "measurement", r => new DbFloat(((dynamic)r).Measurement) },
                { "score", r => new DbFloat(((dynamic)r).Score) }
            }
        };

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.BulkInsert(transaction, records, config);
            return true;
        });

        // Assert
        Assert.Equal(3, await GetRowCount(TestTable));
    }

    [Fact]
    public async Task BulkInsert_WithDbInstant_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            event_time TIMESTAMPTZ NOT NULL
        ");

        var baseTime = NodaTime.SystemClock.Instance.GetCurrentInstant();
        var records = new[]
        {
            new { Id = Guid.NewGuid(), EventTime = baseTime },
            new { Id = Guid.NewGuid(), EventTime = baseTime.Plus(NodaTime.Duration.FromMinutes(10)) },
            new { Id = Guid.NewGuid(), EventTime = baseTime.Plus(NodaTime.Duration.FromMinutes(20)) }
        };

        var config = new BulkInsertConfig<object>
        {
            Table = TestTable,
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", r => new DbGuid(((dynamic)r).Id) },
                { "event_time", r => new DbInstant(((dynamic)r).EventTime) }
            }
        };

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.BulkInsert(transaction, records, config);
            return true;
        });

        // Assert
        Assert.Equal(3, await GetRowCount(TestTable));
    }

    [Fact]
    public async Task BulkInsert_WithDbInt_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UUID PRIMARY KEY,
            counter INTEGER NOT NULL,
            quantity INTEGER NOT NULL
        ");

        var records = new[]
        {
            new { Id = Guid.NewGuid(), Counter = 10, Quantity = 100 },
            new { Id = Guid.NewGuid(), Counter = 20, Quantity = 200 },
            new { Id = Guid.NewGuid(), Counter = 30, Quantity = 300 }
        };

        var config = new BulkInsertConfig<object>
        {
            Table = TestTable,
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", r => new DbGuid(((dynamic)r).Id) },
                { "counter", r => new DbInt(((dynamic)r).Counter) },
                { "quantity", r => new DbInt(((dynamic)r).Quantity) }
            }
        };

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.BulkInsert(transaction, records, config);
            return true;
        });

        // Assert
        Assert.Equal(3, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, "counter = 10 AND quantity = 100"));
        Assert.True(await RowExists(TestTable, "counter = 20 AND quantity = 200"));
        Assert.True(await RowExists(TestTable, "counter = 30 AND quantity = 300"));
    }
}
