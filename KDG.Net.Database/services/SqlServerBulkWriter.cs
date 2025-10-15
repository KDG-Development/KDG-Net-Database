using KDG.Database.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using DbType = KDG.Database.Common.DbType;

namespace KDG.Database.Services;

public class SqlServerBulkWriter : IBulkWriter
{
    private readonly DataTable _dataTable;
    private readonly DataRow _currentRow;
    private int _currentColumnIndex;

    public SqlServerBulkWriter(DataTable dataTable, DataRow currentRow)
    {
        _dataTable = dataTable;
        _currentRow = currentRow;
        _currentColumnIndex = 0;
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
            DbType.TimestampTz => SqlDbType.DateTime2,
            DbType.Timestamp => SqlDbType.DateTime2,
            DbType.Uuid => SqlDbType.UniqueIdentifier,
            DbType.Jsonb => SqlDbType.NVarChar,
            DbType.Json => SqlDbType.NVarChar,
            _ => throw new ArgumentException($"Unsupported DbType: {dbType}")
        };
    }

    public void Write<A>(A value, DbType dbType)
    {
        if (_currentColumnIndex >= _dataTable.Columns.Count)
        {
            throw new InvalidOperationException("Attempted to write more columns than defined in the DataTable");
        }

        _currentRow[_currentColumnIndex] = (object?)value ?? DBNull.Value;
        _currentColumnIndex++;
    }

    public void WriteNull()
    {
        if (_currentColumnIndex >= _dataTable.Columns.Count)
        {
            throw new InvalidOperationException("Attempted to write more columns than defined in the DataTable");
        }

        _currentRow[_currentColumnIndex] = DBNull.Value;
        _currentColumnIndex++;
    }

    public void ResetColumnIndex()
    {
        _currentColumnIndex = 0;
    }
}

