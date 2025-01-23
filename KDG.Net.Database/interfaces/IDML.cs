using KDG.Database.Common;

namespace KDG.Database.Interfaces;

public interface IBulkConfigBase<T> {
    public string Table { get; set; }
    public Dictionary<string, Func<T, ADbValue>> Fields { get; set; }
}

public interface IConfigBase<T> {
    public string Table { get; set; }
    public Dictionary<string, Func<T, ADbValue>> Fields { get; set; }
    public T Data { get; set;}
}

public interface IInsertConfig<T> : IConfigBase<T> {}

public interface IDeleteConfig<T> : IConfigBase<T> {}

public interface IUpdateConfig<T> : IConfigBase<T> {
    public Dictionary<string, Func<T, ADbValue>> Key { get; set; }
    public Dictionary<string, Func<T, ADbValue>> Predicates { get; set; }
}
public interface IUpsertConfig<T> : IConfigBase<T> {
    public Dictionary<string, Func<T, ADbValue>> Key { get; set; }
}

public interface IBulkInsertConfig<T> : IBulkConfigBase<T> {}

public interface IBulkWriter {
    public void Write<A>(A value,NpgsqlTypes.NpgsqlDbType npgsqlDbType);
    public void WriteNull();
}
