using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using KDG.Database.Interfaces;
using KDG.Common;
using System.Xml.Serialization;
using System.Runtime.CompilerServices;
using Npgsql;
namespace KDG.Database.Common;
public class DbNullable<T> : ADbValue
{
    private readonly Option<T> Value;
    private readonly Func<T, ADbValue> Mapper;

    public DbNullable(Option<T> value, Func<T, ADbValue> mapper)
    {
        Value = value;
        Mapper = mapper;
    }

    public override void HandleWrite(IBulkWriter writer)
    {
        Value.Match(
            some: (value) => {
                var dbValue = Mapper(value);
                dbValue.HandleWrite(writer);
                return true;
            },
            none: () => {
                writer.WriteNull();
                return true;
            }
        );
    }

    public override NpgsqlParameter AddParameter(string parameterName, IQueryBuilder builder) {
        return Value.Match(
            some: (value) => {
                return Mapper(value).AddParameter(parameterName, builder);
            },
            none: () => {
                return builder.AddNull(parameterName);
            }
        );
    }
}


