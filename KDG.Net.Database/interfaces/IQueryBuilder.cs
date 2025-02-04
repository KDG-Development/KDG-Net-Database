using Npgsql;

namespace KDG.Database.Interfaces;

public interface IQueryBuilder
{
    public NpgsqlParameter AddParameter(string parameterName, object value,NpgsqlTypes.NpgsqlDbType npgsqlDbType);
    public NpgsqlParameter AddNull(string parameterName);
}
