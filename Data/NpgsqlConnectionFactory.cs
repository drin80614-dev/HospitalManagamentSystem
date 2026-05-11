using Microsoft.Extensions.Options;
using Npgsql;

namespace HospitalManagamentSystem.Data;

public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _configuration;
    private readonly SupabaseOptions _options;

    public NpgsqlConnectionFactory(IConfiguration configuration, IOptions<SupabaseOptions> options)
    {
        _configuration = configuration;
        _options = options.Value;
    }

    public System.Data.IDbConnection CreateConnection()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION") ??
            Environment.GetEnvironmentVariable("SUPABASE_POSTGRES_CONNECTION_STRING") ??
            _configuration.GetConnectionString("SupabasePostgres") ??
            _options.PostgresConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var dbPassword =
                Environment.GetEnvironmentVariable("SUPABASE_DB_PASSWORD") ??
                _configuration["Supabase:DbPassword"];

            if (!string.IsNullOrWhiteSpace(dbPassword) && !string.IsNullOrWhiteSpace(_options.ProjectId))
            {
                var builder = new NpgsqlConnectionStringBuilder
                {
                    Host = $"db.{_options.ProjectId}.supabase.co",
                    Port = 5432,
                    Database = "postgres",
                    Username = "postgres",
                    Password = dbPassword,
                    SslMode = SslMode.Require,
                    Timeout = 15,
                    CommandTimeout = 30
                };

                connectionString = builder.ConnectionString;
            }
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Supabase PostgreSQL connection is missing. Set SUPABASE_DB_CONNECTION, ConnectionStrings:SupabasePostgres, or SUPABASE_DB_PASSWORD for the configured Supabase project.");
        }

        return new NpgsqlConnection(NormalizeConnectionString(connectionString));
    }

    private static string NormalizeConnectionString(string connectionString)
    {
        var trimmed = connectionString.Trim();
        if (!trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return WithSafeTimeouts(trimmed);
        }

        var uri = new Uri(trimmed);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? string.Empty),
            Password = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty),
            SslMode = SslMode.Require,
            Timeout = 15,
            CommandTimeout = 30
        };

        foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var keyValue = part.Split('=', 2);
            var key = Uri.UnescapeDataString(keyValue[0]);
            var value = Uri.UnescapeDataString(keyValue.ElementAtOrDefault(1) ?? string.Empty);

            if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase) &&
                Enum.TryParse<SslMode>(value, ignoreCase: true, out var sslMode))
            {
                builder.SslMode = sslMode;
            }
        }

        return builder.ConnectionString;
    }

    private static string WithSafeTimeouts(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (builder.Timeout <= 0)
        {
            builder.Timeout = 15;
        }

        if (builder.CommandTimeout <= 0)
        {
            builder.CommandTimeout = 30;
        }

        return builder.ConnectionString;
    }
}
