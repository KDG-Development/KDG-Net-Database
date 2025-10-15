using NodaTime;
using Microsoft.Data.SqlClient;
using System.Data;
using KDG.Database.Services;
using KDG.Database.Common;
using KDG.Database.Interfaces;

namespace KDG.Database;

public class SqlServer : DML.SqlServer {
    public string ConnectionString { get; set; }
    
    public SqlServer(string connectionString) {
        this.ConnectionString = connectionString;
    }

    [Obsolete("Use WithConnection<A>(Func<SqlConnection, Task<A>> execute) instead.")]
    public Task<A> withConnection<A>(Func<SqlConnection, Task<A>> execute) => WithConnection(execute);
    
    public async Task<A> WithConnection<A>(Func<SqlConnection, Task<A>> execute) {
        A result;
        
        // Register NodaTime type handlers for Dapper
        Dapper.SqlMapper.AddTypeHandler(new KDG.Database.TypeMappers.SqlServer.NodaTimeInstant());
        Dapper.SqlMapper.AddTypeHandler(new KDG.Database.TypeMappers.SqlServer.NodaTimeLocalDate());
        Dapper.SqlMapper.AddTypeHandler(new KDG.Database.TypeMappers.SqlServer.NodaTimeNullableInstant());
        Dapper.SqlMapper.AddTypeHandler(new KDG.Database.TypeMappers.SqlServer.NodaTimeNullableLocalDate());

        using var connection = new SqlConnection(this.ConnectionString);
        {
            await connection.OpenAsync();
            result = await execute(connection);
            await connection.CloseAsync();
        }

        return result;
    }

    public async Task<SqlTransaction> GetTransaction() {
        // Register NodaTime type handlers for Dapper
        Dapper.SqlMapper.AddTypeHandler(new KDG.Database.TypeMappers.SqlServer.NodaTimeInstant());
        Dapper.SqlMapper.AddTypeHandler(new KDG.Database.TypeMappers.SqlServer.NodaTimeLocalDate());
        Dapper.SqlMapper.AddTypeHandler(new KDG.Database.TypeMappers.SqlServer.NodaTimeNullableInstant());
        Dapper.SqlMapper.AddTypeHandler(new KDG.Database.TypeMappers.SqlServer.NodaTimeNullableLocalDate());

        var connection = new SqlConnection(this.ConnectionString);
        await connection.OpenAsync();
        return connection.BeginTransaction();
    }

    private async Task<A> MapConnectionToTransaction<A>(SqlConnection conn, Func<SqlTransaction, Task<A>> execute) {
        using var transaction = conn.BeginTransaction();
        {
            try {
                var result = await execute(transaction);
                await transaction.CommitAsync();
                return result;
            } catch (Exception) {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    [Obsolete("Use WithTransaction<A>(Func<SqlTransaction, Task<A>> execute) instead.")]
    public Task<A> withTransaction<A>(Func<SqlTransaction, Task<A>> execute) => WithTransaction(execute);
    
    public async Task<A> WithTransaction<A>(Func<SqlTransaction, Task<A>> execute) {
        return await WithConnection((conn) => {
            return MapConnectionToTransaction(conn, execute);
        });
    }

    private string _escape(string s)
    {
        return s.Replace("]", "]]");
    }

    private Task _executeBulk<A>(SqlTransaction transaction, IEnumerable<A> records, DML.BulkInsertConfig<A> config)
    {
        if (transaction.Connection == null)
        {
            throw new NullReferenceException("Transaction doesn't have a connection.");
        }

        var recordList = records.ToList();
        if (recordList.Count == 0)
        {
            return Task.CompletedTask; // No records to insert
        }

        // Create a DataTable to hold the data
        var dataTable = new DataTable();
        
        // Add columns to DataTable with proper types by examining the first record
        var firstRecord = recordList[0];
        foreach (var field in config.Fields)
        {
            var dbValue = field.Value(firstRecord);
            var columnType = GetColumnTypeFromDbValue(dbValue);
            dataTable.Columns.Add(field.Key, columnType);
        }

        // Populate the DataTable with records
        foreach (var record in recordList)
        {
            var row = dataTable.NewRow();
            
            int columnIndex = 0;
            foreach (var field in config.Fields)
            {
                var dbValue = field.Value(record);
                var value = GetValueFromDbValue(dbValue);
                row[columnIndex] = value ?? DBNull.Value;
                columnIndex++;
            }
            
            dataTable.Rows.Add(row);
        }

        // Use SqlBulkCopy to insert the data
        using var bulkCopy = new SqlBulkCopy(transaction.Connection, SqlBulkCopyOptions.Default, transaction);
        bulkCopy.DestinationTableName = config.Table;
        
        // Map columns
        foreach (var field in config.Fields)
        {
            bulkCopy.ColumnMappings.Add(field.Key, field.Key);
        }

        bulkCopy.WriteToServer(dataTable);
        return Task.CompletedTask;
    }

    private Type GetColumnTypeFromDbValue(ADbValue dbValue)
    {
        // Handle DbNullable types by checking the generic type parameter
        if (dbValue.GetType().IsGenericType && dbValue.GetType().GetGenericTypeDefinition() == typeof(DbNullable<>))
        {
            var genericArg = dbValue.GetType().GetGenericArguments()[0];
            return genericArg;
        }

        return dbValue switch
        {
            DbGuid => typeof(Guid),
            DbString => typeof(string),
            DbNumeric => typeof(decimal),
            DbBool => typeof(bool),
            DbFloat => typeof(float),
            DbInt => typeof(int),
            DbDate => typeof(DateTime),
            DbInstant => typeof(DateTimeOffset),
            DbJson => typeof(string),
            _ => typeof(object)
        };
    }

    private object? GetValueFromDbValue(ADbValue dbValue)
    {
        // Handle DbNullable types by mimicking what HandleWrite does
        if (dbValue.GetType().IsGenericType && dbValue.GetType().GetGenericTypeDefinition() == typeof(DbNullable<>))
        {
            // For SQL Server bulk insert, we need to extract the actual value from Option<T>
            // We'll do this by calling the same Match pattern that HandleWrite uses
            
            // Create a temporary in-memory writer to capture what HandleWrite would do
            var capturedValue = new CaptureDbValue();
            
            // Call HandleWrite which will use Match internally
            try
            {
                dbValue.HandleWrite(capturedValue);
                return capturedValue.Value;
            }
            catch
            {
                return null;
            }
        }

        return dbValue switch
        {
            DbGuid guid => typeof(DbGuid).GetField("_value", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(guid),
            DbString str => str.Value,
            DbNumeric num => num.Value,
            DbBool b => typeof(DbBool).GetField("_value", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(b),
            DbFloat f => typeof(DbFloat).GetField("_value", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(f),
            DbInt i => typeof(DbInt).GetField("_value", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(i),
            DbDate date => ((NodaTime.LocalDate)typeof(DbDate).GetField("_value", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(date)!).ToDateTimeUnspecified(),
            DbInstant instant => ((NodaTime.Instant)typeof(DbInstant).GetField("_value", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(instant)!).ToDateTimeOffset(),
            DbJson json => json.Value,
            _ => null
        };
    }
    
    // Helper class to capture the value that would be written by HandleWrite
    private class CaptureDbValue : IBulkWriter
    {
        public object? Value { get; private set; }
        
        public void Write<A>(A value, Common.DbType dbType)
        {
            Value = value;
        }
        
        public void WriteNull()
        {
            Value = null;
        }
    }

    public async Task BulkInsert<A>(SqlTransaction transaction, IEnumerable<A> records, DML.BulkInsertConfig<A> config)
    {
        await _executeBulk(transaction, records, config);
    }

    // parameter utilities
    private static string PredicateParam(string s) {
        return $"_predicate_{s}";
    }

    public async Task Insert<T>(
        SqlTransaction transaction,
        DML.InsertConfig<T> config
    ) {
        if (transaction.Connection == null)
        {
            throw new InvalidOperationException("Transaction does not have an associated connection.");
        }
        using var command = transaction.Connection.CreateCommand();
        command.Transaction = transaction;

        var builder = new SqlServerQueryBuilder(command);

        var parameters = config.Fields.Select(field => {
            field.Value(config.Data)
                .AddParameter(field.Key, builder);
            return $"@{field.Key}";
        }).ToList();

        command.CommandText = $@"
            INSERT INTO [{_escape(config.Table)}] (
                {string.Join(",", config.Fields.Select(field => $"[{_escape(field.Key)}]"))}
            ) VALUES (
                {string.Join(",", parameters)}
            )
        ";

        await command.ExecuteNonQueryAsync();
    }

    public async Task Update<T>(
        SqlTransaction transaction,
        DML.UpdateConfig<T> config
    ) {
        if (transaction.Connection == null)
        {
            throw new InvalidOperationException("Transaction does not have an associated connection.");
        }
        using var command = transaction.Connection.CreateCommand();
        command.Transaction = transaction;

        // add all params
        var allFields = config.Fields
            .Concat(config.Predicates.Select(predicate => new KeyValuePair<string, Func<T, ADbValue>>(PredicateParam(predicate.Key), predicate.Value)))
            .Concat(config.Key)
            .GroupBy(pair => pair.Key)
            .ToDictionary(group => group.Key, group => group.First().Value);

        var builder = new SqlServerQueryBuilder(command);
        var parameters = allFields.Select(field => {
            field.Value(config.Data)
                .AddParameter(field.Key, builder);
            return field.Key;
        }).ToList();

        var keyClause =
            string.Join(" AND ", config.Key.Select(field => $"[{_escape(field.Key)}] = @{field.Key}"));
        var predicateClause =
            string.Join(" AND ", config.Predicates.Select(predicate => $"[{_escape(predicate.Key)}] = @{PredicateParam(predicate.Key)}"));


        var composedWhereClause =
            predicateClause.Length > 0
            ? string.Join(" AND ", keyClause, predicateClause)
            : keyClause;

        command.CommandText = $@"
            UPDATE [{_escape(config.Table)}]
            SET {string.Join(",", config.Fields.Select(field => $"[{_escape(field.Key)}] = @{field.Key}"))}
            WHERE {composedWhereClause}
        ";
        await command.ExecuteNonQueryAsync();
    }

    public async Task Upsert<T>(
        SqlTransaction transaction,
        DML.UpsertConfig<T> config
    ) {
        if (transaction.Connection == null)
        {
            throw new InvalidOperationException("Transaction does not have an associated connection.");
        }
        using var command = transaction.Connection.CreateCommand();
        command.Transaction = transaction;

        var builder = new SqlServerQueryBuilder(command);

        var parameters = config.Fields.Select(field => {
            field.Value(config.Data)
                .AddParameter(field.Key, builder);
            return field.Key;
        }).ToList();

        // SQL Server uses MERGE statement for upsert
        // Build the list of non-key fields for the UPDATE clause
        var updateFields = config.Fields
            .Where(field => !config.Key.Select(x => x.Key).Contains(field.Key))
            .ToList();

        var updateClause = updateFields.Count > 0
            ? $"UPDATE SET {string.Join(",", updateFields.Select(field => $"target.[{_escape(field.Key)}] = source.[{_escape(field.Key)}]"))}"
            : "UPDATE SET target.[{_escape(config.Fields.First().Key)}] = source.[{_escape(config.Fields.First().Key)}]"; // Handle case where all fields are keys

        command.CommandText = $@"
            MERGE INTO [{_escape(config.Table)}] AS target
            USING (SELECT {string.Join(",", config.Fields.Select(x => $"@{x.Key} AS [{_escape(x.Key)}]"))}) AS source
            ON ({string.Join(" AND ", config.Key.Select(x => $"target.[{_escape(x.Key)}] = source.[{_escape(x.Key)}]"))})
            WHEN MATCHED THEN
                {updateClause}
            WHEN NOT MATCHED THEN
                INSERT ({string.Join(",", config.Fields.Select(x => $"[{_escape(x.Key)}]"))})
                VALUES ({string.Join(",", config.Fields.Select(x => $"source.[{_escape(x.Key)}]"))});
        ";
        await command.ExecuteNonQueryAsync();
    }

    public async Task Delete<T>(
        SqlTransaction transaction,
        DML.DeleteConfig<T> config
    ) {
        if (transaction.Connection == null)
        {
            throw new InvalidOperationException("Transaction does not have an associated connection.");
        }
        using var command = transaction.Connection.CreateCommand();
        command.Transaction = transaction;

        var builder = new SqlServerQueryBuilder(command);

        // Add all parameters first
        foreach (var field in config.Fields) {
            field.Value(config.Data)
                .AddParameter(field.Key, builder);
        }

        command.CommandText = $@"
            DELETE FROM [{_escape(config.Table)}]
            WHERE {string.Join(" AND ", config.Fields.Select(field => $"[{_escape(field.Key)}] = @{field.Key}"))}
        ";

        await command.ExecuteNonQueryAsync();
    }
}

