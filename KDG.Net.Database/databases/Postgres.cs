using NodaTime;
using Npgsql;
using System.Data;
using KDG.Database.Services;
using System.Text.RegularExpressions;
using KDG.Database.Common;
using KDG.Database.Interfaces;

namespace KDG.Database;

public class PostgreSQL : DML.PostgreSQL {
    public string ConnectionString { get; set; }
    public PostgreSQL(string connectionString) {
        this.ConnectionString = connectionString;
    }
    [Obsolete("Use WithConnection<A>(Func<NpgsqlConnection, Task<A>> execute) instead.")]
    public Task<A> withConnection<A>(Func<NpgsqlConnection, Task<A>> execute) => WithConnection(execute);
    private async Task<NpgsqlConnection> CreateConnection() {
        Dapper.SqlMapper.AddTypeHandler(new KDG.Database.TypeMappers.PostgreSQL.NodaTimeInstant());
        Dapper.SqlMapper.AddTypeHandler(new KDG.Database.TypeMappers.PostgreSQL.NodaTimeLocalDate());
        Dapper.SqlMapper.AddTypeHandler(new KDG.Database.TypeMappers.PostgreSQL.NodaTimeNullableInstant());
        Dapper.SqlMapper.AddTypeHandler(new KDG.Database.TypeMappers.PostgreSQL.NodaTimeNullableLocalDate());

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(this.ConnectionString);
        dataSourceBuilder.UseNodaTime();
        await using var dataSource = dataSourceBuilder.Build();
        var connection = dataSource.CreateConnection();
        await connection.OpenAsync();
        return connection;
    }

    public async Task<A> WithConnection<A>(Func<NpgsqlConnection, Task<A>> execute) {
        A result;
        await using var connection = await CreateConnection();
        result = await execute(connection);
        await connection.CloseAsync();
        return result;
    }

    public async Task<NpgsqlTransaction> GetTransaction() {
        var connection = await CreateConnection();
        return connection.BeginTransaction();
    }

    private async Task<A> MapConnectionToTransaction<A>(NpgsqlConnection conn, Func<NpgsqlTransaction, Task<A>> execute) {
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
    [Obsolete("Use WithTransaction<A>(Func<NpgsqlTransaction, Task<A>> execute) instead.")]
    public Task<A> withTransaction<A>(Func<NpgsqlTransaction, Task<A>> execute) => WithTransaction(execute);
    public async Task<A> WithTransaction<A>(Func<NpgsqlTransaction, Task<A>> execute) {
        return await WithConnection((conn) => {
            return MapConnectionToTransaction(conn, execute);
        });
    }



    private string _escape(string s)
    {
      return s.Replace(@"""", @"""""");
    }

    private Task _executeBulk<A>(NpgsqlTransaction transaction, IEnumerable<A> records, DML.BulkInsertConfig<A> config)
    {
        var fields = string.Join(",", config.Fields.Select((v) => _escape(v.Key)));

        if (transaction.Connection == null)
        {
            throw new NullReferenceException("Transaction doesn't have a connection.");
        }
        using var writer = transaction.Connection.BeginBinaryImport(string.Format(@"
            COPY {0}(
            {1}
            ) FROM STDIN(FORMAT BINARY)
        ", _escape(config.Table), fields));
        var bulkWriter = new BulkWriter(writer);
        foreach (var record in records)
        {
            writer.StartRow();
            foreach (var field in config.Fields)
            {
                var t = field.Value(record);
                t.HandleWrite(bulkWriter);
            }
        }
        writer.Complete();
        writer.Dispose();
        return Task.CompletedTask;
    }

    public async Task BulkInsert<A>(Npgsql.NpgsqlTransaction transaction, IEnumerable<A> records, DML.BulkInsertConfig<A> config)
    {
        await _executeBulk(transaction, records, config);
    }

    public async Task BulkUpsert<A>(
        Npgsql.NpgsqlTransaction transaction,
        IEnumerable<A> records,
        DML.BulkUpsertConfig<A> config
    ) {
        if (transaction.Connection == null)
        {
            throw new InvalidOperationException("Transaction does not have an associated connection.");
        }

        var tempTable = $"__{config.Table}";

        // Drop temp table if exists
        using (var dropCommand = transaction.Connection.CreateCommand())
        {
            dropCommand.Transaction = transaction;
            dropCommand.CommandText = $"DROP TABLE IF EXISTS {_escape(tempTable)}";
            await dropCommand.ExecuteNonQueryAsync();
        }

        // Create temp table
        using (var createCommand = transaction.Connection.CreateCommand())
        {
            createCommand.Transaction = transaction;
            createCommand.CommandText = $"CREATE TEMP TABLE {_escape(tempTable)} AS (SELECT * FROM {_escape(config.Table)} LIMIT 0)";
            await createCommand.ExecuteNonQueryAsync();
        }

        // Build update clause
        var updateClause = string.Join(",", config.Fields
            .Where(field => !config.Ignore.Contains(field.Key))
            .Select(field => $"{_escape(field.Key)} = EXCLUDED.{_escape(field.Key)}"));

        // Build key clause
        var keyClause = string.Join(",", config.Key.Select(k => _escape(k)));

        // Build fields list
        var fields = string.Join(",", config.Fields.Select(f => _escape(f.Key)));

        // Bulk insert into temp table
        await _executeBulk(transaction, records, new DML.BulkInsertConfig<A> {
            Table = tempTable,
            Fields = config.Fields
        });

        // Perform upsert
        using (var upsertCommand = transaction.Connection.CreateCommand())
        {
            upsertCommand.Transaction = transaction;
            upsertCommand.CommandText = $@"
                INSERT INTO {_escape(config.Table)} (
                    {fields}
                )
                SELECT {fields} FROM {_escape(tempTable)}
                ON CONFLICT ({keyClause}) DO UPDATE SET
                    {updateClause}";
            await upsertCommand.ExecuteNonQueryAsync();
        }

        // Drop temp table
        using (var dropCommand = transaction.Connection.CreateCommand())
        {
            dropCommand.Transaction = transaction;
            dropCommand.CommandText = $"DROP TABLE {_escape(tempTable)}";
            await dropCommand.ExecuteNonQueryAsync();
        }
    }

    // parameter utilities
    private static string PredicateParam(string s) {
        return $"_predicate_{s}";
    }

    public async Task Insert<T>(
        Npgsql.NpgsqlTransaction transaction,
        DML.InsertConfig<T> config
    ) {
        if (transaction.Connection == null)
        {
            throw new InvalidOperationException("Transaction does not have an associated connection.");
        }
        using var command = transaction.Connection.CreateCommand();
        command.Transaction = transaction;

        var builder = new QueryBuilder(command);

        var parameters = config.Fields.Select(field => {
            field.Value(config.Data)
                .AddParameter(field.Key, builder);
            return $"@{field.Key}";
        }).ToList();

        command.CommandText = $@"
            insert into {config.Table} (
                {string.Join(",", config.Fields.Select(field => field.Key))}
            ) values (
                {string.Join(",", parameters)}
            )
        ";

        await command.ExecuteNonQueryAsync();
    }

    public async Task Update<T>(
        Npgsql.NpgsqlTransaction transaction,
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

        var builder = new QueryBuilder(command);
        var parameters = allFields.Select(field => {
            field.Value(config.Data)
                .AddParameter(field.Key, builder);
            return field.Key;
        }).ToList();

        var keyClause =
            string.Join(" and ", config.Key.Select(field => $"{field.Key} = @{field.Key}"));
        var predicateClause =
            string.Join(" and ", config.Predicates.Select(predicate => $"{predicate.Key} = @{PredicateParam(predicate.Key)}"));


        var composedWhereClause =
            predicateClause.Length > 0
            ? string.Join(" and ", keyClause, predicateClause)
            : keyClause;

        command.CommandText = $@"
            update {config.Table}
            set {string.Join(",", config.Fields.Select(field => $"{field.Key} = @{field.Key}"))}
            where {composedWhereClause}
        ";
        await command.ExecuteNonQueryAsync();
    }
    public async Task Upsert<T>(
        Npgsql.NpgsqlTransaction transaction,
        DML.UpsertConfig<T> config
    ) {
        if (transaction.Connection == null)
        {
            throw new InvalidOperationException("Transaction does not have an associated connection.");
        }
        using var command = transaction.Connection.CreateCommand();
        command.Transaction = transaction;

        var builder = new QueryBuilder(command);

        var parameters = config.Fields.Select(field => {
            field.Value(config.Data)
                .AddParameter(field.Key, builder);
            return field.Key;
        }).ToList();

        command.CommandText = $@"
            insert into
            {config.Table}
            ({string.Join(",", config.Fields.Select(x => x.Key))})
            values
            ({string.Join(",", config.Fields.Select(x => $"@{x.Key}"))})
            on conflict
            ({string.Join(",", config.Key.Select(x => x.Key))})
            do update set
            {
                string.Join(",", config.Fields.Select(field => {
                    if (config.Key.Select(x => x.Key).Contains(field.Key)) {
                        // a bit ugly, but i think this is the only safe way to conditionally upsert when we are only updating a single column
                        return config.Fields.Count == 1 ? $"{field.Key} = excluded.{field.Key}" : null;
                    } else {
                        return $"{field.Key} = excluded.{field.Key}";
                    }
                }).Where(field => field != null))
            }
        ";
        await command.ExecuteNonQueryAsync();
    }

    public async Task Delete<T>(
        Npgsql.NpgsqlTransaction transaction,
        DML.DeleteConfig<T> config
    ) {
        if (transaction.Connection == null)
        {
            throw new InvalidOperationException("Transaction does not have an associated connection.");
        }
        using var command = transaction.Connection.CreateCommand();
        command.Transaction = transaction;

        var builder = new QueryBuilder(command);

        var parameters = config.Fields.Select(field => {
            field.Value(config.Data)
                .AddParameter(field.Key, builder);
            return field.Key;
        }).ToList();

        command.CommandText = $@"
            delete from {config.Table}
            where {string.Join(" and ", config.Fields.Select(predicate => $"{predicate.Key} = @{predicate.Key}"))}
        ";

        await command.ExecuteNonQueryAsync();
    }
}