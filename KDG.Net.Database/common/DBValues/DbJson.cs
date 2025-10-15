using System.Text.Json;
using KDG.Database.Interfaces;
using System.Data;

namespace KDG.Database.Common;

public class DbJson : ADbValue
{
    public string Value { get; }

    public DbJson(string jsonString)
    {
        Value = jsonString;
    }

    public DbJson(object obj, JsonSerializerOptions? options = null)
    {
        Value = JsonSerializer.Serialize(obj, options);
    }

    public override void HandleWrite(IBulkWriter writer)
    {
        writer.Write(Value, DbType.Jsonb);
    }

    public override IDbDataParameter AddParameter(string parameterName, IQueryBuilder builder)
    {
        return builder.AddParameter(parameterName, Value, DbType.Jsonb);
    }
}


