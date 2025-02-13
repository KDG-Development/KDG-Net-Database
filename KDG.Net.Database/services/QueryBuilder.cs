using KDG.Database.Common;
using KDG.Database.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using NpgsqlTypes;

namespace KDG.Database.Services;

public class QueryBuilder : IQueryBuilder
{
    private readonly NpgsqlCommand _command;

    public QueryBuilder(NpgsqlCommand command)
    {
        _command = command;
    }

    public NpgsqlParameter AddNull(string parameterName)
    {
        var parameter = new NpgsqlParameter(parameterName, DBNull.Value);
        _command.Parameters.Add(parameter);
        return parameter;
    }

    public NpgsqlParameter AddParameter(string parameterName, object value, NpgsqlDbType npgsqlDbType)
    {
        var parameter = new NpgsqlParameter(parameterName, npgsqlDbType);
        parameter.Value = value;
        _command.Parameters.Add(parameter);
        return parameter;
    }
}
