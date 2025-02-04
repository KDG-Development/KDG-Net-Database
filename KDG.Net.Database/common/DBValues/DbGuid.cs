using KDG.Database.Interfaces;
using Npgsql;

namespace KDG.Database.Common;

public class DbGuid : ADbValue {
    private Guid _value;

    public DbGuid(Guid value) {
        _value = value;
    }

    public override void HandleWrite(IBulkWriter writer) {
        writer.Write(_value, NpgsqlTypes.NpgsqlDbType.Uuid);
    }

    public override NpgsqlParameter AddParameter(string parameterName, IQueryBuilder builder) {
        return builder.AddParameter(parameterName, _value, NpgsqlTypes.NpgsqlDbType.Uuid);
    }
}
