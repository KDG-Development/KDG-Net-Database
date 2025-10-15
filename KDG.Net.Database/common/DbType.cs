namespace KDG.Database.Common;

public enum DbType
{
    // String types
    Text,
    
    // Numeric types
    Numeric,
    Integer,
    BigInteger,
    Real,
    DoublePrecision,
    
    // Boolean
    Boolean,
    
    // Date/Time types
    Date,
    TimestampTz,
    Timestamp,
    
    // UUID/GUID
    Uuid,
    
    // JSON
    Jsonb,
    Json
}

