using KDG.Database.Interfaces;
using Npgsql;

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
        writer.Write(Value, NpgsqlTypes.NpgsqlDbType.Numeric);
    }

    public override NpgsqlParameter AddParameter(string parameterName, IQueryBuilder builder) {
        return builder.AddParameter(parameterName, Value);
    }
}
