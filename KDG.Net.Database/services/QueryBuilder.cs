using KDG.Database.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Npgsql;
using NpgsqlTypes;
using DbType = KDG.Database.Common.DbType;

namespace KDG.Database.Services;

public class QueryBuilder : IQueryBuilder
{
    private readonly NpgsqlCommand _command;

    public QueryBuilder(NpgsqlCommand command)
    {
        _command = command;
    }

    public IDbDataParameter AddNull(string parameterName)
    {
        var parameter = new NpgsqlParameter(parameterName, DBNull.Value);
        _command.Parameters.Add(parameter);
        return parameter;
    }

    private NpgsqlDbType MapDbType(DbType dbType)
    {
        return dbType switch
        {
            DbType.Text => NpgsqlDbType.Text,
            DbType.Numeric => NpgsqlDbType.Numeric,
            DbType.Integer => NpgsqlDbType.Integer,
            DbType.BigInteger => NpgsqlDbType.Bigint,
            DbType.Real => NpgsqlDbType.Real,
            DbType.DoublePrecision => NpgsqlDbType.Double,
            DbType.Boolean => NpgsqlDbType.Boolean,
            DbType.Date => NpgsqlDbType.Date,
            DbType.TimestampTz => NpgsqlDbType.TimestampTz,
            DbType.Timestamp => NpgsqlDbType.Timestamp,
            DbType.Uuid => NpgsqlDbType.Uuid,
            DbType.Jsonb => NpgsqlDbType.Jsonb,
            DbType.Json => NpgsqlDbType.Json,
            _ => throw new ArgumentException($"Unsupported DbType: {dbType}")
        };
    }

    public IDbDataParameter AddParameter(string parameterName, object value, DbType dbType)
    {
        var npgsqlDbType = MapDbType(dbType);
        var parameter = new NpgsqlParameter(parameterName, npgsqlDbType);
        parameter.Value = value;
        _command.Parameters.Add(parameter);
        return parameter;
    }
}
