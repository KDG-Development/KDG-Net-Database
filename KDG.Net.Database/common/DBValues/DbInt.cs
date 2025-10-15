using KDG.Database.Interfaces;
using System.Data;

namespace KDG.Database.Common;

public class DbInt : ADbValue {
    private int _value;

    public DbInt(int value) {
        _value = value;
    }

    public override void HandleWrite(IBulkWriter writer) {
        writer.Write(_value, DbType.Integer);
    }

    public override IDbDataParameter AddParameter(string parameterName, IQueryBuilder builder) {
        return builder.AddParameter(parameterName, _value, DbType.Integer);
    }
}
