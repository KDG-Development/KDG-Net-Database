using KDG.Database.Common;

namespace KDG.Database.Interfaces;


public interface IConfigBase<T> {
    public string Table { get; set; }
    public Dictionary<string, Func<T, object>> Fields { get; set; }
    public T Data { get; set;}
}

public interface IInsertConfig<T> : IConfigBase<T> {}

public interface IDeleteConfig<T> : IConfigBase<T> {}

public interface IUpdateConfig<T> : IConfigBase<T> {
    public IEnumerable<string> Key { get; set;}
    public Dictionary<string, Func<T, object>> Predicates { get; set; }
}
