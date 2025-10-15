using KDG.Database.Interfaces;
using System.Data;

namespace KDG.Database.Common;

public abstract class ADbValue {
    public abstract void HandleWrite(IBulkWriter writer);
    public abstract IDbDataParameter AddParameter(string parameterName, IQueryBuilder builder);
}
