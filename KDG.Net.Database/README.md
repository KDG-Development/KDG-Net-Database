# Getting started

## PostgreSQL

### Connecting to the database

1. Initialize your database connection
```
var db = new KDG.Database.Database.PostgreSQL("your-connection-string");
```
2. Fetch data using `withConnection` and `withTransaction`
```
var data = db.withConnection(async conn => {
  var result = await conn.QueryAsync("select * from table");
  return result;
});
```

### DML Operations

The `KDG.Database.Database.PostgreSQL` class has corresponding insert, update, and delete methods
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

```
var db = new KDG.Database.PostgreSQL("connection-string");
await db.withTransaction(t => {
  db.Insert(
    t,
    new KDG.Database.DML.InsertConfig<UserModel>{
      Table="your-table",
      Data=New UserModel(),
      Fields=new Dictionary<string,Func<UserModel,object>>{
        "id", x => x.id,
        "email", x => x.email
      },
    }
  );
  return true;
})
```

## Support

For support, please open an issue on our [GitHub Issues page](https://github.com/KDG-Development/KDG-Net-Database/issues) and provide your questions or feedback. We strive to address all inquiries promptly.

## Contributing

To contribute to this project, please follow these steps:

1. Fork the repository to your own GitHub account.
2. Make your changes and commit them to your fork.
3. Submit a pull request to the original repository with a clear description of what your changes do and why they should be included.
