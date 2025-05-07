using KDG.Database.Interfaces;
using Npgsql;
using NpgsqlTypes;

namespace KDG.Database.Common
{
    public class DbInstant : ADbValue
    {
        private readonly NodaTime.Instant _value;

        public DbInstant(NodaTime.Instant value)
        {
            _value = value;
        }

        public override NpgsqlParameter AddParameter(string name, IQueryBuilder builder)
        {
            return builder.AddParameter(name, _value, NpgsqlTypes.NpgsqlDbType.TimestampTz);
        }

        public override void HandleWrite(IBulkWriter writer)
        {
            writer.Write(_value, NpgsqlDbType.TimestampTz);
        }
    }
}
