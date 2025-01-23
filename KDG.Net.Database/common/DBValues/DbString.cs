using KDG.Database.Interfaces;
using Npgsql;
namespace KDG.Database.Common;

public class DbString : ADbValue
{
    public string Value { get; }

    public DbString(string value)
    {
        Value = value;
    }

    public override void HandleWrite(IBulkWriter writer)
    {
        writer.Write(Value, NpgsqlTypes.NpgsqlDbType.Text);
    }

    public override NpgsqlParameter AddParameter(string parameterName, IQueryBuilder builder) {
        return builder.AddParameter(parameterName, Value);
    }
}
