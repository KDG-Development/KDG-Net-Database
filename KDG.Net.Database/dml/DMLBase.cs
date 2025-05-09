using KDG.Database.Common;
using KDG.Database.Interfaces;

namespace KDG.Database.DML;


public class ConfigBase<T> : IConfigBase<T> {
    public required string Table { get; set; }
    public required Dictionary<string, Func<T, ADbValue>> Fields { get; set; }
    public required T Data { get; set;}
}

public class BulkConfigBase<T> : IBulkConfigBase<T> {
    public required string Table { get; set; }
    public required Dictionary<string, Func<T, ADbValue>> Fields { get; set; }
}

public class InsertConfig<T> : ConfigBase<T>, IInsertConfig<T> {}

public class DeleteConfig<T> : ConfigBase<T>,IDeleteConfig<T> {}

public class UpdateConfig<T> : ConfigBase<T>,IUpdateConfig<T> {
    public required Dictionary<string, Func<T, ADbValue>> Key { get; set; }
    public Dictionary<string, Func<T, ADbValue>> Predicates { get; set; } = new Dictionary<string, Func<T, ADbValue>>{};
}
public class UpsertConfig<T> : ConfigBase<T>,IUpsertConfig<T> {
    public required Dictionary<string, Func<T, ADbValue>> Key { get; set; }
}

public class BulkInsertConfig<T> : BulkConfigBase<T>, IBulkInsertConfig<T> {}

public class BulkUpsertConfig<T> : BulkConfigBase<T>, IBulkUpsertConfig<T> {
    public required IEnumerable<string> Key { get; set; }
    public IEnumerable<string> Ignore { get; set; } = new List<string>();
}
