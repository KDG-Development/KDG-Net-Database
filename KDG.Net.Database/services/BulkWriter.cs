using KDG.Database.Common;
using KDG.Database.Interfaces;
using Npgsql;

namespace KDG.Database.Services;

public class BulkWriter : IBulkWriter
{
    private NpgsqlBinaryImporter _writer;

    public BulkWriter(NpgsqlBinaryImporter writer)
    {
        _writer = writer;
    }

    public void Write<A>(A value,NpgsqlTypes.NpgsqlDbType npgsqlDbType)
    {
        _writer.Write(value, npgsqlDbType);
    }

    public void WriteNull()
    {
        _writer.WriteNull();
    }
}
