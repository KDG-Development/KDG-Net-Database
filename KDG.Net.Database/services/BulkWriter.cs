using KDG.Database.Interfaces;
using Npgsql;
using NpgsqlTypes;
using System;
using DbType = KDG.Database.Common.DbType;

namespace KDG.Database.Services;

public class BulkWriter : IBulkWriter
{
    private NpgsqlBinaryImporter _writer;

    public BulkWriter(NpgsqlBinaryImporter writer)
    {
        _writer = writer;
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

    public void Write<A>(A value, DbType dbType)
    {
        var npgsqlDbType = MapDbType(dbType);
        _writer.Write(value, npgsqlDbType);
    }

    public void WriteNull()
    {
        _writer.WriteNull();
    }
}
