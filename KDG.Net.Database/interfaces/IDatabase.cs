namespace KDG.Database.Interfaces;
public interface IDatabase<TConnection, TTransaction> {
    string ConnectionString { get; set; }
    [Obsolete("Use WithConnection<A>(Func<TConnection, Task<A>> execute) instead.")]
    Task<A> withConnection<A>(Func<TConnection, Task<A>> execute);
    Task<A> WithConnection<A>(Func<TConnection, Task<A>> execute);
    [Obsolete("Use WithTransaction<A>(Func<TTransaction, Task<A>> execute) instead.")]
    Task<A> withTransaction<A>(Func<TTransaction, Task<A>> execute);
    Task<A> WithTransaction<A>(Func<TTransaction, Task<A>> execute);
    Task<TTransaction> GetTransaction();
}
