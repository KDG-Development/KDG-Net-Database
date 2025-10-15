using KDG.Database.Common;
using KDG.Database.DML;
using Xunit;

namespace KDG.Database.Tests.Integration.SqlServer;

public class BulkInsertOperationTests : SqlServerIntegrationTestBase
{
    private const string TestTable = "bulk_insert_test";

    public class TestRecord
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public int Value { get; set; }
    }

    [Fact]
    public async Task BulkInsert_WithSmallBatch_InsertsAllRecords()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            name NVARCHAR(255) NOT NULL,
            value INT NOT NULL
        ");

        var records = new List<TestRecord>
        {
            new TestRecord { Id = Guid.NewGuid(), Name = "Record1", Value = 100 },
            new TestRecord { Id = Guid.NewGuid(), Name = "Record2", Value = 200 },
            new TestRecord { Id = Guid.NewGuid(), Name = "Record3", Value = 300 }
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
        Assert.True(await RowExists(TestTable, "name = 'Record1' AND value = 100"));
        Assert.True(await RowExists(TestTable, "name = 'Record2' AND value = 200"));
        Assert.True(await RowExists(TestTable, "name = 'Record3' AND value = 300"));
    }

    [Fact]
    public async Task BulkInsert_WithLargeBatch_InsertsAllRecords()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            name NVARCHAR(255) NOT NULL,
            value INT NOT NULL
        ");

        var records = Enumerable.Range(1, 1000).Select(i => new TestRecord
        {
            Id = Guid.NewGuid(),
            Name = $"Record{i}",
            Value = i
        }).ToList();

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
        Assert.Equal(1000, await GetRowCount(TestTable));
        Assert.True(await RowExists(TestTable, "name = 'Record1' AND value = 1"));
        Assert.True(await RowExists(TestTable, "name = 'Record500' AND value = 500"));
        Assert.True(await RowExists(TestTable, "name = 'Record1000' AND value = 1000"));
    }

    [Fact]
    public async Task BulkInsert_WithVariousDataTypes_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            name NVARCHAR(255) NOT NULL,
            value INT NOT NULL,
            active BIT NOT NULL,
            price DECIMAL(18,2) NOT NULL
        ");

        var records = new List<object>
        {
            new { Id = Guid.NewGuid(), Name = "Product1", Value = 10, Active = true, Price = 99.99m },
            new { Id = Guid.NewGuid(), Name = "Product2", Value = 20, Active = false, Price = 149.50m },
            new { Id = Guid.NewGuid(), Name = "Product3", Value = 30, Active = true, Price = 75.25m }
        };

        var config = new BulkInsertConfig<object>
        {
            Table = TestTable,
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", r => new DbGuid(((dynamic)r).Id) },
                { "name", r => new DbString(((dynamic)r).Name) },
                { "value", r => new DbNumeric(((dynamic)r).Value) },
                { "active", r => new DbBool(((dynamic)r).Active) },
                { "price", r => new DbNumeric(((dynamic)r).Price) }
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
        Assert.True(await RowExists(TestTable, "name = 'Product1' AND active = 1 AND price = 99.99"));
        Assert.True(await RowExists(TestTable, "name = 'Product2' AND active = 0 AND price = 149.50"));
    }

    [Fact]
    public async Task BulkInsert_WithTransactionRollback_DoesNotInsert()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            name NVARCHAR(255) NOT NULL,
            value INT NOT NULL
        ");

        var records = new List<TestRecord>
        {
            new TestRecord { Id = Guid.NewGuid(), Name = "Record1", Value = 100 },
            new TestRecord { Id = Guid.NewGuid(), Name = "Record2", Value = 200 }
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

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await Database.WithTransaction<bool>(async transaction =>
            {
                await Database.BulkInsert(transaction, records, config);
                throw new Exception("Intentional rollback");
#pragma warning disable CS0162 // Unreachable code detected
                return true;
#pragma warning restore CS0162 // Unreachable code detected
            });
        });

        // Assert - no records should be inserted due to rollback
        Assert.Equal(0, await GetRowCount(TestTable));
    }

    [Fact]
    public async Task BulkInsert_WithEmptyCollection_DoesNotFail()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            name NVARCHAR(255) NOT NULL,
            value INT NOT NULL
        ");

        var records = new List<TestRecord>();

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
        Assert.Equal(0, await GetRowCount(TestTable));
    }

    [Fact]
    public async Task BulkInsert_WithDbFloat_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            temperature REAL NOT NULL,
            humidity REAL NOT NULL
        ");

        var records = new[]
        {
            new { Id = Guid.NewGuid(), Temperature = 22.5f, Humidity = 45.0f },
            new { Id = Guid.NewGuid(), Temperature = 23.7f, Humidity = 50.2f },
            new { Id = Guid.NewGuid(), Temperature = 21.3f, Humidity = 48.9f }
        };

        var config = new BulkInsertConfig<object>
        {
            Table = TestTable,
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", r => new DbGuid(((dynamic)r).Id) },
                { "temperature", r => new DbFloat(((dynamic)r).Temperature) },
                { "humidity", r => new DbFloat(((dynamic)r).Humidity) }
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
            id UNIQUEIDENTIFIER PRIMARY KEY,
            timestamp DATETIMEOFFSET NOT NULL
        ");

        var baseTime = NodaTime.SystemClock.Instance.GetCurrentInstant();
        var records = new[]
        {
            new { Id = Guid.NewGuid(), Timestamp = baseTime },
            new { Id = Guid.NewGuid(), Timestamp = baseTime.Plus(NodaTime.Duration.FromMinutes(5)) },
            new { Id = Guid.NewGuid(), Timestamp = baseTime.Plus(NodaTime.Duration.FromMinutes(10)) }
        };

        var config = new BulkInsertConfig<object>
        {
            Table = TestTable,
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", r => new DbGuid(((dynamic)r).Id) },
                { "timestamp", r => new DbInstant(((dynamic)r).Timestamp) }
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
            id UNIQUEIDENTIFIER PRIMARY KEY,
            counter INT NOT NULL,
            stock INT NOT NULL
        ");

        var records = new[]
        {
            new { Id = Guid.NewGuid(), Counter = 5, Stock = 50 },
            new { Id = Guid.NewGuid(), Counter = 10, Stock = 100 },
            new { Id = Guid.NewGuid(), Counter = 15, Stock = 150 }
        };

        var config = new BulkInsertConfig<object>
        {
            Table = TestTable,
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", r => new DbGuid(((dynamic)r).Id) },
                { "counter", r => new DbInt(((dynamic)r).Counter) },
                { "stock", r => new DbInt(((dynamic)r).Stock) }
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
        Assert.True(await RowExists(TestTable, "counter = 5 AND stock = 50"));
        Assert.True(await RowExists(TestTable, "counter = 10 AND stock = 100"));
        Assert.True(await RowExists(TestTable, "counter = 15 AND stock = 150"));
    }
}

