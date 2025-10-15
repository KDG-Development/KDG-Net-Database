using KDG.Database.Common;
using KDG.Database.DML;
using KDG.Common;
using Xunit;

namespace KDG.Database.Tests.Integration.SqlServer;

public class DbNullableOperationTests : SqlServerIntegrationTestBase
{
    private const string TestTable = "nullable_test";

    [Fact]
    public async Task Insert_WithDbNullableString_SomeValue_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            name NVARCHAR(255),
            description NVARCHAR(MAX)
        ");

        var testId = Guid.NewGuid();
        string? name = "Test Name";
        string? description = "Description";

        var config = new InsertConfig<object>
        {
            Table = TestTable,
            Data = new { },
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", _ => new DbGuid(testId) },
                { "name", _ => new DbNullable<string>(name.ToOption(), s => new DbString(s)) },
                { "description", _ => new DbNullable<string>(description.ToOption(), s => new DbString(s)) }
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
        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND name = 'Test Name'"));
    }

    [Fact]
    public async Task Insert_WithDbNullableString_NoneValue_InsertsNull()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            name NVARCHAR(255),
            optional_field NVARCHAR(MAX)
        ");

        var testId = Guid.NewGuid();
        string? name = "Test";
        string? optionalField = null;

        var config = new InsertConfig<object>
        {
            Table = TestTable,
            Data = new { },
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", _ => new DbGuid(testId) },
                { "name", _ => new DbNullable<string>(name.ToOption(), s => new DbString(s)) },
                { "optional_field", _ => new DbNullable<string>(optionalField.ToOption(), s => new DbString(s)) }
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
        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND optional_field IS NULL"));
    }

    [Fact]
    public async Task Insert_WithDbNullableInt_SomeValue_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            count INT,
            optional_count INT
        ");

        var testId = Guid.NewGuid();
        int? count = 42;
        int? optionalCount = null;

        var config = new InsertConfig<object>
        {
            Table = TestTable,
            Data = new { },
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", _ => new DbGuid(testId) },
                { "count", _ => new DbNullable<int>(count.ToOption(), i => new DbInt(i)) },
                { "optional_count", _ => new DbNullable<int>(optionalCount.ToOption(), i => new DbInt(i)) }
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
        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND count = 42 AND optional_count IS NULL"));
    }

    [Fact]
    public async Task Insert_WithDbNullableDecimal_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            price DECIMAL(18,2),
            discount DECIMAL(18,2)
        ");

        var testId = Guid.NewGuid();
        decimal? price = 99.99m;
        decimal? discount = null;

        var config = new InsertConfig<object>
        {
            Table = TestTable,
            Data = new { },
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", _ => new DbGuid(testId) },
                { "price", _ => new DbNullable<decimal>(price.ToOption(), d => new DbNumeric(d)) },
                { "discount", _ => new DbNullable<decimal>(discount.ToOption(), d => new DbNumeric(d)) }
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
        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND discount IS NULL"));
    }

    [Fact]
    public async Task Insert_WithDbNullableGuid_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            parent_id UNIQUEIDENTIFIER,
            related_id UNIQUEIDENTIFIER
        ");

        var testId = Guid.NewGuid();
        Guid? parentId = Guid.NewGuid();
        Guid? relatedId = null;

        var config = new InsertConfig<object>
        {
            Table = TestTable,
            Data = new { },
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", _ => new DbGuid(testId) },
                { "parent_id", _ => new DbNullable<Guid>(parentId.ToOption(), g => new DbGuid(g)) },
                { "related_id", _ => new DbNullable<Guid>(relatedId.ToOption(), g => new DbGuid(g)) }
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
        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND parent_id = '{parentId}' AND related_id IS NULL"));
    }

    [Fact]
    public async Task BulkInsert_WithDbNullable_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            name NVARCHAR(255) NOT NULL,
            description NVARCHAR(MAX),
            count INT
        ");

        string? desc1 = "Has description";
        string? desc2 = null;
        string? desc3 = "Another description";
        int? count1 = 10;
        int? count2 = 20;
        int? count3 = null;

        var records = new[]
        {
            new { Id = Guid.NewGuid(), Name = "First", Description = (string?)desc1, Count = count1 },
            new { Id = Guid.NewGuid(), Name = "Second", Description = (string?)desc2, Count = count2 },
            new { Id = Guid.NewGuid(), Name = "Third", Description = (string?)desc3, Count = count3 }
        };

        var config = new BulkInsertConfig<object>
        {
            Table = TestTable,
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", r => new DbGuid(((dynamic)r).Id) },
                { "name", r => new DbString(((dynamic)r).Name) },
                { "description", r => new DbNullable<string>(((string?)((dynamic)r).Description).ToOption(), s => new DbString(s)) },
                { "count", r => new DbNullable<int>(((int?)((dynamic)r).Count).ToOption(), i => new DbInt(i)) }
            }
        };

        // Act
        await Database.WithTransaction(async transaction =>
        {
            await Database.BulkInsert(transaction, records, config);
            return true;
        });

        // Assert
        var rowCount = await GetRowCount(TestTable);
        Assert.Equal(3, rowCount);
        
        // Check if any rows exist at all
        Assert.True(await RowExists(TestTable, "name = 'First'"), "Row with name 'First' not found");
        Assert.True(await RowExists(TestTable, "name = 'Second'"), "Row with name 'Second' not found");
        Assert.True(await RowExists(TestTable, "name = 'Third'"), "Row with name 'Third' not found");
        
        // Now check the full conditions
        Assert.True(await RowExists(TestTable, "name = 'First' AND description IS NOT NULL AND count = 10"), "First row doesn't match expected conditions");
        Assert.True(await RowExists(TestTable, "name = 'Second' AND description IS NULL AND count = 20"), "Second row doesn't match expected conditions");
        Assert.True(await RowExists(TestTable, "name = 'Third' AND description IS NOT NULL AND count IS NULL"), "Third row doesn't match expected conditions");
    }

    [Fact]
    public async Task Update_WithDbNullable_UpdatesCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            name NVARCHAR(255) NOT NULL,
            optional_value NVARCHAR(MAX)
        ");

        var testId = Guid.NewGuid();
        string? initialValue = "Initial";
        string? nullValue = null;

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
                    { "name", _ => new DbString("Test") },
                    { "optional_value", _ => new DbNullable<string>(initialValue.ToOption(), s => new DbString(s)) }
                }
            });
            return true;
        });

        // Act - Update to null
        await Database.WithTransaction(async transaction =>
        {
            await Database.Update(transaction, new UpdateConfig<object>
            {
                Table = TestTable,
                Data = new { },
                Key = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "id", _ => new DbGuid(testId) }
                },
                Fields = new Dictionary<string, Func<object, ADbValue>>
                {
                    { "optional_value", _ => new DbNullable<string>(nullValue.ToOption(), s => new DbString(s)) }
                }
            });
            return true;
        });

        // Assert
        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND optional_value IS NULL"));
    }

    [Fact]
    public async Task Insert_WithDbNullableBool_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            is_active BIT,
            is_verified BIT
        ");

        var testId = Guid.NewGuid();
        bool? isActive = true;
        bool? isVerified = null;

        var config = new InsertConfig<object>
        {
            Table = TestTable,
            Data = new { },
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", _ => new DbGuid(testId) },
                { "is_active", _ => new DbNullable<bool>(isActive.ToOption(), b => new DbBool(b)) },
                { "is_verified", _ => new DbNullable<bool>(isVerified.ToOption(), b => new DbBool(b)) }
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
        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND is_active = 1 AND is_verified IS NULL"));
    }

    [Fact]
    public async Task Insert_WithDbNullableFloat_InsertsCorrectly()
    {
        // Arrange
        await CreateTestTable(TestTable, @"
            id UNIQUEIDENTIFIER PRIMARY KEY,
            temperature REAL,
            humidity REAL
        ");

        var testId = Guid.NewGuid();
        float? temperature = 22.5f;
        float? humidity = null;

        var config = new InsertConfig<object>
        {
            Table = TestTable,
            Data = new { },
            Fields = new Dictionary<string, Func<object, ADbValue>>
            {
                { "id", _ => new DbGuid(testId) },
                { "temperature", _ => new DbNullable<float>(temperature.ToOption(), f => new DbFloat(f)) },
                { "humidity", _ => new DbNullable<float>(humidity.ToOption(), f => new DbFloat(f)) }
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
        Assert.True(await RowExists(TestTable, $"id = '{testId}' AND humidity IS NULL"));
    }
}
