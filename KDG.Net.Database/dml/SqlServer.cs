using KDG.Database.Interfaces;
using Microsoft.Data.SqlClient;

namespace KDG.Database.DML;

public interface SqlServer : IDatabase<SqlConnection, SqlTransaction> {
    public Task Insert<T>(SqlTransaction transaction, InsertConfig<T> config);
    public Task Update<T>(SqlTransaction transaction, UpdateConfig<T> config);
    public Task Upsert<T>(SqlTransaction transaction, UpsertConfig<T> config);
    public Task Delete<T>(SqlTransaction transaction, DeleteConfig<T> config);
    public Task BulkInsert<A>(SqlTransaction transaction, IEnumerable<A> records, DML.BulkInsertConfig<A> config);
}



