namespace KDG.Database.Common;

public interface IDbValue { }

public abstract class DbValue : IDbValue
{
    private DbValue() { }

    public sealed class DbGuid : DbValue
    {
        public System.Guid Value { get; }
        public DbGuid(System.Guid value) => Value = value;
    }

    public sealed class DbDate : DbValue
    {
        public NodaTime.LocalDate Value { get; }
        public DbDate(NodaTime.LocalDate value) => Value = value;
    }

    public sealed class DbTime : DbValue
    {
        public NodaTime.LocalTime Value { get; }
        public DbTime(NodaTime.LocalTime value) => Value = value;
    }

    public sealed class DbDateTime : DbValue
    {
        public NodaTime.Instant Value { get; }
        public DbDateTime(NodaTime.Instant value) => Value = value;
    }

    public sealed class DbString : DbValue
    {
        public string Value { get; }
        public DbString(string value) => Value = value;
    }

    public sealed class DbInt : DbValue
    {
        public int Value { get; }
        public DbInt(int value) => Value = value;
    }

    public sealed class DbInt64 : DbValue
    {
        public long Value { get; }
        public DbInt64(long value) => Value = value;
    }

    public sealed class DbUInt : DbValue
    {
        public uint Value { get; }
        public DbUInt(uint value) => Value = value;
    }

    public sealed class DbDouble : DbValue
    {
        public double Value { get; }
        public DbDouble(double value) => Value = value;
    }

    public sealed class DbDecimal : DbValue
    {
        public decimal Value { get; }
        public DbDecimal(decimal value) => Value = value;
    }

    public sealed class DbBool : DbValue
    {
        public bool Value { get; }
        public DbBool(bool value) => Value = value;
    }

    // public sealed class DbJson : DbValue
    // {
    //     public Json.Json Value { get; }
    //     public DbJson(Json.Json value) => Value = value;
    // }

    public sealed class DbNull : DbValue { }

    public sealed class DbNullable : DbValue
    {
        public DbValue? Value { get; }
        public DbNullable(DbValue? value) => Value = value;
    }

    // public sealed class DbArrayString : DbValue
    // {
    //     public string[] Value { get; }
    //     public DbArrayString(string[] value) => Value = value;
    // }

    // public sealed class DbEnum : DbValue
    // {
    //     public object Value { get; }
    //     public string Enum { get; }
    //     public DbEnum(object value, string @enum)
    //     {
    //         Value = value;
    //         Enum = @enum;
    //     }
    // }
}