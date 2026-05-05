namespace HospitalManagamentSystem.Data;

public class SupabaseOptions
{
    public string Url { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string AnonKey { get; set; } = string.Empty;
    public string ServiceRoleKey { get; set; } = string.Empty;
    public string PostgresConnectionString { get; set; } = string.Empty;
}
