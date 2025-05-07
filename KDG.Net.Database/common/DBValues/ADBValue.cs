using KDG.Database.Interfaces;
using Npgsql;

namespace KDG.Database.Common;

public abstract class ADbValue {
    public abstract void HandleWrite(IBulkWriter writer);
    public abstract NpgsqlParameter AddParameter(string parameterName, IQueryBuilder builder);
}
