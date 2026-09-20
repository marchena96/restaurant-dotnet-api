using System.Text;
using Microsoft.Data.SqlClient;

namespace RestauranteAPI.Configuration;

public static class RuntimeConfiguration
{
    public const string ConnectionStringName = "ConnectionSql";

    public static JwtSettings GetRequiredJwtSettings(IConfiguration configuration)
    {
        var settings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{JwtSettings.SectionName}' is required.");

        ValidateRequired(settings.SecretKey, $"{JwtSettings.SectionName}:SecretKey");
        ValidateRequired(settings.Issuer, $"{JwtSettings.SectionName}:Issuer");
        ValidateRequired(settings.Audience, $"{JwtSettings.SectionName}:Audience");

        if (Encoding.UTF8.GetByteCount(settings.SecretKey) < 32)
        {
            throw new InvalidOperationException(
                $"Configuration value '{JwtSettings.SectionName}:SecretKey' must contain at least 32 UTF-8 bytes.");
        }

        if (settings.ExpiryInDays is < 1 or > 365)
        {
            throw new InvalidOperationException(
                $"Configuration value '{JwtSettings.SectionName}:ExpiryInDays' must be between 1 and 365.");
        }

        return settings;
    }

    public static string GetRequiredConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        ValidateRequired(connectionString, $"ConnectionStrings:{ConnectionStringName}");

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            ValidateRequired(builder.DataSource, $"ConnectionStrings:{ConnectionStringName} Server");
            ValidateRequired(builder.InitialCatalog, $"ConnectionStrings:{ConnectionStringName} Database");
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"Configuration value 'ConnectionStrings:{ConnectionStringName}' is not a valid SQL Server connection string.",
                exception);
        }

        return connectionString!;
    }

    private static void ValidateRequired(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Configuration value '{key}' is required.");
        }
    }
}
