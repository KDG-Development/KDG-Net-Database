using KDG.Database.Interfaces;
using System.Data;

namespace KDG.Database.Common;

public class DbNumeric : ADbValue
{
    public decimal Value { get; }

    public DbNumeric(decimal value)
    {
        Value = value;
    }

    public override void HandleWrite(IBulkWriter writer)
    {
        writer.Write(Value, DbType.Numeric);
    }

    public override IDbDataParameter AddParameter(string parameterName, IQueryBuilder builder) {
        return builder.AddParameter(parameterName, Value, DbType.Numeric);
    }
}
