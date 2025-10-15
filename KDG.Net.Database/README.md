# Getting started

This library provides a unified interface for database operations supporting both PostgreSQL and SQL Server.

## PostgreSQL

### Connecting to the database

1. Initialize your database connection
```csharp
var db = new KDG.Database.PostgreSQL("your-connection-string");
```

Example connection string:
```
Host=localhost;Port=5432;Database=mydb;Username=user;Password=password
```

2. Fetch data using `WithConnection` and `WithTransaction`
```csharp
var data = await db.WithConnection(async conn => {
  var result = await conn.QueryAsync("select * from table");
  return result;
});
```

### DML Operations

The `KDG.Database.PostgreSQL` class has corresponding insert, update, upsert, delete, and bulk insert methods
- Insert
```

  KDG.Database.Database.PostgreSQL.Insert<T>(
    Npgsql.NpgsqlTransaction t,
    KDG.Database.DML.InsertConfig<T> data
  )
```
- Update
```

  KDG.Database.Database.PostgreSQL.Update<T>(
    Npgsql.NpgsqlTransaction t,
    KDG.Database.DML.UpdateConfig<T> data
  )
```
- Upsert
```

  KDG.Database.Database.PostgreSQL.Upsert<T>(
    Npgsql.NpgsqlTransaction t,
    KDG.Database.DML.UpsertConfig<T> data
  )
```
- Delete
```

  KDG.Database.Database.PostgreSQL.Delete<T>(
    Npgsql.NpgsqlTransaction t,
    KDG.Database.DML.DeleteConfig<T> data
  )
```

Here's what a full example might look like:

```csharp
var db = new KDG.Database.PostgreSQL("connection-string");
await db.WithTransaction(async t => {
  await db.Insert(
    t,
    new KDG.Database.DML.InsertConfig<UserModel>{
      Table = "your-table",
      Data = new UserModel(),
      Fields = new Dictionary<string, Func<UserModel, ADbValue>>{
        { "id", x => new DbGuid(x.Id) },
        { "email", x => new DbString(x.Email) }
      },
    }
  );
  return true;
});
```

## SQL Server

### Connecting to the database

1. Initialize your database connection
```csharp
var db = new KDG.Database.SqlServer("your-connection-string");
```

Example connection string:
```
Server=localhost;Database=mydb;User Id=sa;Password=YourPassword;TrustServerCertificate=True
```

2. Fetch data using `WithConnection` and `WithTransaction`
```csharp
var data = await db.WithConnection(async conn => {
  var result = await conn.QueryAsync("SELECT * FROM [table]");
  return result;
});
```

### DML Operations

The `KDG.Database.SqlServer` class has corresponding insert, update, upsert (MERGE), delete, and bulk insert methods.

#### Insert Example
```csharp
await db.WithTransaction(async transaction => {
  await db.Insert(transaction, new InsertConfig<UserModel> {
    Table = "Users",
    Data = new UserModel { Id = Guid.NewGuid(), Email = "user@example.com" },
    Fields = new Dictionary<string, Func<UserModel, ADbValue>> {
      { "id", u => new DbGuid(u.Id) },
      { "email", u => new DbString(u.Email) }
    }
  });
  return true;
});
```

#### Upsert Example (MERGE)
```csharp
await db.WithTransaction(async transaction => {
  await db.Upsert(transaction, new UpsertConfig<UserModel> {
    Table = "Users",
    Data = user,
    Key = new Dictionary<string, Func<UserModel, ADbValue>> {
      { "id", u => new DbGuid(u.Id) }
    },
    Fields = new Dictionary<string, Func<UserModel, ADbValue>> {
      { "id", u => new DbGuid(u.Id) },
      { "email", u => new DbString(u.Email) },
      { "name", u => new DbString(u.Name) }
    }
  });
  return true;
});
```

#### Bulk Insert Example (SqlBulkCopy)
```csharp
var users = GetLargeUserList(); // Returns IEnumerable<UserModel>

await db.WithTransaction(async transaction => {
  await db.BulkInsert(transaction, users, new BulkInsertConfig<UserModel> {
    Table = "Users",
    Fields = new Dictionary<string, Func<UserModel, ADbValue>> {
      { "id", u => new DbGuid(u.Id) },
      { "email", u => new DbString(u.Email) },
      { "name", u => new DbString(u.Name) }
    }
  });
  return true;
});
```

### Supported Data Types

Both PostgreSQL and SQL Server implementations support the following `ADbValue` types:

- `DbString` - Text/NVARCHAR
- `DbNumeric` - Numeric/Decimal types (decimal, int, float)
- `DbGuid` - UUID/UNIQUEIDENTIFIER
- `DbBool` - Boolean/BIT
- `DbDate` - Date types (NodaTime.LocalDate)
- `DbInstant` - Timestamp types (NodaTime.Instant)
- `DbJson` - JSON/JSONB (PostgreSQL) or NVARCHAR(MAX) (SQL Server)
- `DbFloat` - Real/Float types
- `DbNullable<T>` - Nullable versions of any type

## Support

For support, please open an issue on our [GitHub Issues page](https://github.com/KDG-Development/KDG-Net-Database/issues) and provide your questions or feedback. We strive to address all inquiries promptly.

## Contributing

To contribute to this project, please follow these steps:

1. Fork the repository to your own GitHub account.
2. Make your changes and commit them to your fork.
3. Submit a pull request to the original repository with a clear description of what your changes do and why they should be included.
