using KDG.Database.Interfaces;
using System.Data;

namespace KDG.Database.Common;

public class DbGuid : ADbValue {
    private Guid _value;

    public DbGuid(Guid value) {
        _value = value;
    }

    public override void HandleWrite(IBulkWriter writer) {
        writer.Write(_value, DbType.Uuid);
    }

    public override IDbDataParameter AddParameter(string parameterName, IQueryBuilder builder) {
        return builder.AddParameter(parameterName, _value, DbType.Uuid);
    }
}
