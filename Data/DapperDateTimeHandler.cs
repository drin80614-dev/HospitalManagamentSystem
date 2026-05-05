using System.Data;
using Dapper;

namespace HospitalManagamentSystem.Data;

public sealed class DapperDateTimeHandler : SqlMapper.TypeHandler<DateTime>
{
    public override DateTime Parse(object value)
    {
        return value switch
        {
            DateTime dateTime => dateTime,
            DateOnly dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
            _ => Convert.ToDateTime(value)
        };
    }

    public override void SetValue(IDbDataParameter parameter, DateTime value)
    {
        parameter.Value = value;
    }
}
