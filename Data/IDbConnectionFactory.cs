using System.Data;

namespace HospitalManagamentSystem.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
