using KDG.Database.Interfaces;
using System.Data;

namespace KDG.Database.Common
{
    public class DbInstant : ADbValue
    {
        private readonly NodaTime.Instant _value;

        public DbInstant(NodaTime.Instant value)
        {
            _value = value;
        }

        public override IDbDataParameter AddParameter(string name, IQueryBuilder builder)
        {
            return builder.AddParameter(name, _value, DbType.TimestampTz);
        }

        public override void HandleWrite(IBulkWriter writer)
        {
            writer.Write(_value, DbType.TimestampTz);
        }
    }
}
