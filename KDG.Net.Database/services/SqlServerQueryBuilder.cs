using KDG.Database.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using DbType = KDG.Database.Common.DbType;

namespace KDG.Database.Services;

public class SqlServerQueryBuilder : IQueryBuilder
{
    private readonly SqlCommand _command;

    public SqlServerQueryBuilder(SqlCommand command)
    {
        _command = command;
    }

    public IDbDataParameter AddNull(string parameterName)
    {
        var parameter = new SqlParameter(parameterName, DBNull.Value);
        _command.Parameters.Add(parameter);
        return parameter;
    }

    private SqlDbType MapDbType(DbType dbType)
    {
        return dbType switch
        {
            DbType.Text => SqlDbType.NVarChar,
            DbType.Numeric => SqlDbType.Decimal,
            DbType.Integer => SqlDbType.Int,
            DbType.BigInteger => SqlDbType.BigInt,
            DbType.Real => SqlDbType.Real,
            DbType.DoublePrecision => SqlDbType.Float,
            DbType.Boolean => SqlDbType.Bit,
            DbType.Date => SqlDbType.Date,
            DbType.TimestampTz => SqlDbType.DateTimeOffset,
            DbType.Timestamp => SqlDbType.DateTime2,
            DbType.Uuid => SqlDbType.UniqueIdentifier,
            DbType.Jsonb => SqlDbType.NVarChar, // SQL Server uses nvarchar(max) for JSON
            DbType.Json => SqlDbType.NVarChar,
            _ => throw new ArgumentException($"Unsupported DbType: {dbType}")
        };
    }

    public IDbDataParameter AddParameter(string parameterName, object value, DbType dbType)
    {
        var sqlDbType = MapDbType(dbType);
        var parameter = new SqlParameter(parameterName, sqlDbType);
        
        // For JSON and Text types that might be large, set size to -1 (max)
        if (dbType == DbType.Jsonb || dbType == DbType.Json || dbType == DbType.Text)
        {
            parameter.Size = -1; // nvarchar(max)
        }
        
        parameter.Value = value;
        _command.Parameters.Add(parameter);
        return parameter;
    }
}

