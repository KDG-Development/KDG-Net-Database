using KDG.Database.Interfaces;
using System.Data;

namespace KDG.Database.Common;

public class DbFloat : ADbValue {
    private float _value;

    public DbFloat(float value) {
        _value = value;
    }

    public override void HandleWrite(IBulkWriter writer) {
        writer.Write(_value, DbType.Real);
    }

    public override IDbDataParameter AddParameter(string parameterName, IQueryBuilder builder) {
        return builder.AddParameter(parameterName, _value, DbType.Real);
    }
}
