# Getting started
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

## Support

For support, please open an issue on our [GitHub Issues page](https://github.com/KDG-Development/KDG-Net-Database/issues) and provide your questions or feedback. We strive to address all inquiries promptly.

## Contributing

To contribute to this project, please follow these steps:

1. Fork the repository to your own GitHub account.
2. Make your changes and commit them to your fork.
3. Submit a pull request to the original repository with a clear description of what your changes do and why they should be included.
