using KDG.Database.Interfaces;
using Npgsql;

namespace KDG.Database.Common;

public class DbInt : ADbValue {
    private int _value;

    public DbInt(int value) {
        _value = value;
    }

    public override void HandleWrite(IBulkWriter writer) {
        writer.Write(_value, NpgsqlTypes.NpgsqlDbType.Integer);
    }

    public override NpgsqlParameter AddParameter(string parameterName, IQueryBuilder builder) {
        return builder.AddParameter(parameterName, _value, NpgsqlTypes.NpgsqlDbType.Integer);
    }
}
