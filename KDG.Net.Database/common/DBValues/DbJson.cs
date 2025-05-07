using System.Text.Json;
using KDG.Database.Common;
using KDG.Database.Interfaces;
using Npgsql;
using NpgsqlTypes;

namespace KDG.Database.Common;

public class DbJson : ADbValue
{
    private readonly string _value;

    public DbJson(string value)
    {
        _value = value;
    }

    public override void HandleWrite(IBulkWriter writer)
    {
        writer.Write(_value, NpgsqlDbType.Jsonb);
    }

    public override NpgsqlParameter AddParameter(string parameterName, IQueryBuilder builder)
    {
        return builder.AddParameter(parameterName, _value, NpgsqlDbType.Jsonb);
    }
}


