using KDG.Database.Interfaces;
using System.Data;

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
        writer.Write(Value, DbType.Text);
    }

    public override IDbDataParameter AddParameter(string parameterName, IQueryBuilder builder) {
        return builder.AddParameter(parameterName, Value, DbType.Text);
    }
}
