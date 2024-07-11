namespace KDG.Database.DML;

public interface PostgreSQL {
    public Task Insert<T>(Npgsql.NpgsqlTransaction transaction, InsertConfig<T> config);
    public Task Update<T>(Npgsql.NpgsqlTransaction transaction, UpdateConfig<T> config);
    public Task Upsert<T>(Npgsql.NpgsqlTransaction transaction, UpsertConfig<T> config);
    public Task Delete<T>(Npgsql.NpgsqlTransaction transaction, DeleteConfig<T> config);
}