using System.Data;
using Dapper;

namespace HospitalManagamentSystem.Data;

public sealed class DapperTimeSpanHandler : SqlMapper.TypeHandler<TimeSpan>
{
    public override TimeSpan Parse(object value)
    {
        return value switch
        {
            TimeSpan timeSpan => timeSpan,
            TimeOnly timeOnly => timeOnly.ToTimeSpan(),
            _ => TimeSpan.Parse(value.ToString() ?? "00:00:00")
        };
    }

    public override void SetValue(IDbDataParameter parameter, TimeSpan value)
    {
        parameter.Value = value;
    }
}
