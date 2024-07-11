namespace KDG.Database.Interfaces;
public interface IDatabase<TConnection, TTransaction> {
    string ConnectionString { get; set; }
    Task<A> withConnection<A>(Func<TConnection, Task<A>> execute);
    Task<A> withTransaction<A>(Func<TTransaction, Task<A>> execute);
}
