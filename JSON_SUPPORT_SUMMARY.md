# JSON Support for PostgreSQL Database Operations

## Summary

Added comprehensive support for inserting JSON data into PostgreSQL databases using the `DbJson` type.

## Changes Made

### 1. Enhanced `DbJson` Class (`KDG.Net.Database/common/DBValues/DbJson.cs`)

**Features:**
- Added public `Value` property for accessing the JSON string
- Added constructor accepting raw JSON strings: `new DbJson(string jsonString)`
- Added constructor accepting any object with automatic serialization: `new DbJson(object obj, JsonSerializerOptions? options = null)`
- Uses PostgreSQL's JSONB type for efficient storage and querying

**Usage Examples:**

```csharp
// From object (automatic serialization)
var profile = new { Name = "John", Age = 30, Email = "john@example.com" };
var dbValue = new DbJson(profile);

// From JSON string
var jsonString = @"{""name"":""John"",""age"":30}";
var dbValue = new DbJson(jsonString);
```

### 2. Integration Tests

Added comprehensive integration tests across existing test files:

#### `InsertOperationTests.cs` (3 new tests)
- `Insert_WithJsonFromObject_InsertsCorrectly` - Tests object serialization
- `Insert_WithJsonFromString_InsertsCorrectly` - Tests raw JSON string insertion
- `Insert_WithComplexNestedJson_InsertsCorrectly` - Tests nested objects, arrays, and dictionaries

#### `BulkInsertOperationTests.cs` (2 new tests)
- `BulkInsert_WithJsonData_InsertsAllRecords` - Tests bulk insertion of JSON records
- `BulkInsert_WithComplexJsonStructures_InsertsCorrectly` - Tests complex nested structures in bulk

#### `UpdateOperationTests.cs` (1 new test)
- `Update_WithJsonData_UpdatesCorrectly` - Tests updating JSON column data

## Test Results

All 27 integration tests passed, including the 6 new JSON tests:
- **Total Tests:** 27
- **Passed:** 27
- **Failed:** 0
- **Duration:** ~1 second

## PostgreSQL JSONB Features Used

The implementation uses PostgreSQL's JSONB type which provides:
- Efficient binary storage format
- Indexing capabilities
- Rich querying operators (e.g., `->`, `->>`, `@>`)

### Query Examples from Tests:

```sql
-- Access JSON field
WHERE data->>'Name' = 'John Doe'

-- Check array contains value
WHERE data->'Tags' @> '["tech"]'

-- Access nested field
WHERE data->'Metadata'->>'author' = 'John Doe'

-- Type casting
WHERE (data->>'value')::int = 123
```

## Integration with Existing Architecture

The `DbJson` class follows the same pattern as other `DbValue` types:
- Extends `ADbValue`
- Implements `HandleWrite()` for bulk operations
- Implements `AddParameter()` for parameterized queries
- Uses `NpgsqlDbType.Jsonb` for type safety

## Notes

- JSON is stored as JSONB (binary JSON) for performance
- Automatic serialization uses `System.Text.Json`
- Custom `JsonSerializerOptions` can be provided if needed
- Works with all DML operations: Insert, Update, Upsert, BulkInsert, Delete

