using KDG.Database.Interfaces;
using NodaTime;
using System.Data;

namespace KDG.Database.Common;

public class DbDate : ADbValue {
    private LocalDate _value;

    public DbDate(LocalDate value) {
        _value = value;
    }

    public override void HandleWrite(IBulkWriter writer) {
        writer.Write(_value, DbType.Date);
    }

    public override IDbDataParameter AddParameter(string parameterName, IQueryBuilder builder) {
        return builder.AddParameter(parameterName, _value, DbType.Date);
    }
}
