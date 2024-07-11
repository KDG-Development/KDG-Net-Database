using KDG.Database.Interfaces;

namespace KDG.Database.DML;


public class ConfigBase<T> : IConfigBase<T> {
    public required string Table { get; set; }
    public required Dictionary<string, Func<T, object>> Fields { get; set; }
    public required T Data { get; set;}
}

public class InsertConfig<T> : ConfigBase<T>, IInsertConfig<T> {}

public class DeleteConfig<T> : ConfigBase<T>,IDeleteConfig<T> {}

public class UpdateConfig<T> : ConfigBase<T>,IUpdateConfig<T> {
    public required IEnumerable<string> Key { get; set; }
    public Dictionary<string, Func<T, object>> Predicates { get; set; } = new Dictionary<string, Func<T, object>>{};
}