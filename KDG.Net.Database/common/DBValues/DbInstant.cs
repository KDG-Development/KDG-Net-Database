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
            // Convert NodaTime.Instant to DateTimeOffset for SQL Server compatibility
            var dateTimeOffset = _value.ToDateTimeOffset();
            return builder.AddParameter(name, dateTimeOffset, DbType.TimestampTz);
        }

        public override void HandleWrite(IBulkWriter writer)
        {
            writer.Write(_value, DbType.TimestampTz);
        }
    }
}
