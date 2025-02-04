using KDG.Database.Interfaces;
using Npgsql;

namespace KDG.Database.Common;

public class DbFloat : ADbValue {
    private float _value;

    public DbFloat(float value) {
        _value = value;
    }

    public override void HandleWrite(IBulkWriter writer) {
        writer.Write(_value, NpgsqlTypes.NpgsqlDbType.Numeric);
    }

    public override NpgsqlParameter AddParameter(string parameterName, IQueryBuilder builder) {
        return builder.AddParameter(parameterName, _value, NpgsqlTypes.NpgsqlDbType.Numeric);
    }
}
