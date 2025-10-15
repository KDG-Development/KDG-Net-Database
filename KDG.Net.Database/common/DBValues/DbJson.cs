using System.Text.Json;
using KDG.Database.Common;
using KDG.Database.Interfaces;
using Npgsql;
using NpgsqlTypes;

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
        writer.Write(Value, NpgsqlDbType.Jsonb);
    }

    public override NpgsqlParameter AddParameter(string parameterName, IQueryBuilder builder)
    {
        return builder.AddParameter(parameterName, Value, NpgsqlDbType.Jsonb);
    }
}


