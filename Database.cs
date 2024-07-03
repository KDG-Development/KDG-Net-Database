using NodaTime;
using Npgsql;
using System.Data;
using System.Text.RegularExpressions;

namespace KDG.Database;

public interface IDatabase {
    string ConnectionString { get; set; }
    public Task<A> withConnection<A>(Func<NpgsqlConnection, Task<A>> execute);
    public Task<A> withTransaction<A>(Func<NpgsqlTransaction, Task<A>> execute);
}

public class Database {
    
    public class PostgreSQL:IDatabase {
        public string ConnectionString { get; set; }
        public PostgreSQL(string connectionString) {
            this.ConnectionString = connectionString;
        }
        public async Task<A> withConnection<A>(Func<NpgsqlConnection,Task<A>> execute)
        {
        A result;
        Dapper.SqlMapper.AddTypeHandler(new KDG.Database.PostgreSQL.TypeMappers.NodaTimeInstant());
        Dapper.SqlMapper.AddTypeHandler(new KDG.Database.PostgreSQL.TypeMappers.NodaTimeLocalDate());
        Dapper.SqlMapper.AddTypeHandler(new KDG.Database.PostgreSQL.TypeMappers.NodaTimeNullableInstant());
        Dapper.SqlMapper.AddTypeHandler(new KDG.Database.PostgreSQL.TypeMappers.NodaTimeNullableLocalDate());
        // todo: option handlers here or downstream?
        
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(this.ConnectionString);
        dataSourceBuilder.UseNodaTime();
        await using var dataSource = dataSourceBuilder.Build();
        {
            using var connection = dataSource.CreateConnection();
            {
            await connection.OpenAsync();
            result = await execute(connection);
            }
        }

        return result;
        }

        private async Task<A> mapConnectionToTransaction<A>(NpgsqlConnection conn, Func<NpgsqlTransaction, Task<A>> execute)
        {
        using var transaction = conn.BeginTransaction();
        {
            try
            {
            var result = await execute(transaction);
            await transaction.CommitAsync();
            return result;
            }
            catch (Exception)
            {
            await transaction.RollbackAsync();
            throw;
            }
        }
        }

        public async Task<A> withTransaction<A>(Func<NpgsqlTransaction,Task<A>> execute)
        {
        return await withConnection((conn) => {
            return mapConnectionToTransaction(conn,execute);
        });
        }
    }
}
