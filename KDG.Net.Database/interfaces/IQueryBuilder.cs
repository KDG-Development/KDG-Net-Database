using Npgsql;

namespace KDG.Database.Interfaces;

public interface IQueryBuilder
{
    public NpgsqlParameter AddParameter(string parameterName, object value);
    public NpgsqlParameter AddNull(string parameterName);
}
