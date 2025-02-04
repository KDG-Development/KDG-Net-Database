using KDG.Database.Interfaces;
using NodaTime;
using Npgsql;

namespace KDG.Database.Common;

public class DbDate : ADbValue {
    private LocalDate _value;

    public DbDate(LocalDate value) {
        _value = value;
    }

    public override void HandleWrite(IBulkWriter writer) {
        writer.Write(_value, NpgsqlTypes.NpgsqlDbType.Date);
    }

    public override NpgsqlParameter AddParameter(string parameterName, IQueryBuilder builder) {
        return builder.AddParameter(parameterName, _value, NpgsqlTypes.NpgsqlDbType.Date);
    }
}
