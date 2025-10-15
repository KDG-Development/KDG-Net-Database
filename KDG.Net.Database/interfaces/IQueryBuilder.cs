using System.Data;
using DbType = KDG.Database.Common.DbType;

namespace KDG.Database.Interfaces;

public interface IQueryBuilder
{
    public IDbDataParameter AddParameter(string parameterName, object value, DbType dbType);
    public IDbDataParameter AddNull(string parameterName);
}
