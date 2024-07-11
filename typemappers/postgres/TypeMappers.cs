using NodaTime;
using System.Data;

namespace KDG.Database.TypeMappers.PostgreSQL
{
  public class NodaTimeInstant : Dapper.SqlMapper.TypeHandler<NodaTime.Instant>
  {
    public override void SetValue(IDbDataParameter parameter, Instant value)
    {
      parameter.Value = value;
    }
    public override Instant Parse(object value)
    {
      if (value is NodaTime.Instant x)
      {
        return x;
      }
      else
      {
        throw new System.Data.DataException("Not a NodaTime.Instant");
      }
      throw new NotImplementedException();
    }
  }

  public class NodaTimeNullableInstant : Dapper.SqlMapper.TypeHandler<Nullable<NodaTime.Instant>>
  {
    public override void SetValue(IDbDataParameter parameter, Nullable<Instant> value)
    {
      if(value.HasValue)
      {
        parameter.Value = value.Value;
      }
      else
      {
        parameter.Value = null;
      }
    }
    public override Nullable<Instant>Parse(object value)
    {
      if (value is NodaTime.Instant x)
      {
        return new System.Nullable<Instant>(x);
      }
      else
      {
        return new System.Nullable<Instant>();
      }
      throw new NotImplementedException();
    }
  }
    public class NodaTimeLocalDate : Dapper.SqlMapper.TypeHandler<NodaTime.LocalDate>
    {
        public override void SetValue(IDbDataParameter parameter, LocalDate value)
        {
            parameter.Value = value;
        }
        public override LocalDate Parse(object value)
        {
            if (value is NodaTime.LocalDate x)
            {
                return x;
            }
            else
            {
                throw new System.Data.DataException("Not a NodaTime.Instant");
            }
            throw new NotImplementedException();
        }
    }
    public class NodaTimeNullableLocalDate : Dapper.SqlMapper.TypeHandler<Nullable<NodaTime.LocalDate>>
    {
        public override void SetValue(IDbDataParameter parameter, Nullable<LocalDate> value)
        {
            if(value.HasValue)
            {
                parameter.Value = value.Value;
            }
            else
            {
                parameter.Value = null;
            }
        }
        public override Nullable<LocalDate>Parse(object value)
        {
            if (value is NodaTime.LocalDate x)
            {
                return new System.Nullable<LocalDate>(x);
            }
            else
            {
                return new System.Nullable<LocalDate>();
            }
        }
    }
}
