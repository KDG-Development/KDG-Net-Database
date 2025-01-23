using KDG.Database.Interfaces;
using Npgsql;
namespace KDG.Database.Common;

public class DbBool : ADbValue {
    private bool _value;

    public DbBool(bool value) {
        _value = value;
    }

    public override void HandleWrite(IBulkWriter writer) {
        writer.Write(_value, NpgsqlTypes.NpgsqlDbType.Boolean);
    }

    public override NpgsqlParameter AddParameter(string parameterName, IQueryBuilder builder) {
        return builder.AddParameter(parameterName, _value);
    }
}
