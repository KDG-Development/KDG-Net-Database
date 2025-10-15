using NodaTime;
using System.Data;

namespace KDG.Database.TypeMappers.SqlServer
{
    public class NodaTimeInstant : Dapper.SqlMapper.TypeHandler<NodaTime.Instant>
    {
        public override void SetValue(IDbDataParameter parameter, Instant value)
        {
            // Convert NodaTime.Instant to DateTime for SQL Server
            parameter.Value = value.ToDateTimeUtc();
        }

        public override Instant Parse(object value)
        {
            if (value is DateTime dateTime)
            {
                return Instant.FromDateTimeUtc(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
            }
            else
            {
                throw new System.Data.DataException("Not a DateTime that can be converted to NodaTime.Instant");
            }
        }
    }

    public class NodaTimeNullableInstant : Dapper.SqlMapper.TypeHandler<Nullable<NodaTime.Instant>>
    {
        public override void SetValue(IDbDataParameter parameter, Nullable<Instant> value)
        {
            if (value.HasValue)
            {
                parameter.Value = value.Value.ToDateTimeUtc();
            }
            else
            {
                parameter.Value = DBNull.Value;
            }
        }

        public override Nullable<Instant> Parse(object value)
        {
            if (value is DateTime dateTime)
            {
                return new Nullable<Instant>(Instant.FromDateTimeUtc(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)));
            }
            else
            {
                return new Nullable<Instant>();
            }
        }
    }

    public class NodaTimeLocalDate : Dapper.SqlMapper.TypeHandler<NodaTime.LocalDate>
    {
        public override void SetValue(IDbDataParameter parameter, LocalDate value)
        {
            // Convert NodaTime.LocalDate to DateTime for SQL Server
            parameter.Value = value.ToDateTimeUnspecified();
        }

        public override LocalDate Parse(object value)
        {
            if (value is DateTime dateTime)
            {
                return LocalDate.FromDateTime(dateTime);
            }
            else
            {
                throw new System.Data.DataException("Not a DateTime that can be converted to NodaTime.LocalDate");
            }
        }
    }

    public class NodaTimeNullableLocalDate : Dapper.SqlMapper.TypeHandler<Nullable<NodaTime.LocalDate>>
    {
        public override void SetValue(IDbDataParameter parameter, Nullable<LocalDate> value)
        {
            if (value.HasValue)
            {
                parameter.Value = value.Value.ToDateTimeUnspecified();
            }
            else
            {
                parameter.Value = DBNull.Value;
            }
        }

        public override Nullable<LocalDate> Parse(object value)
        {
            if (value is DateTime dateTime)
            {
                return new Nullable<LocalDate>(LocalDate.FromDateTime(dateTime));
            }
            else
            {
                return new Nullable<LocalDate>();
            }
        }
    }
}



