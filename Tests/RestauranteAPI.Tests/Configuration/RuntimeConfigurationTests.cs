using Microsoft.Extensions.Configuration;
using RestauranteAPI.Configuration;

namespace RestauranteAPI.Tests.Configuration;

public sealed class RuntimeConfigurationTests
{
    [Fact]
    public void Missing_jwt_secret_is_rejected()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "issuer",
            ["JwtSettings:Audience"] = "audience",
            ["JwtSettings:ExpiryInDays"] = "7"
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => RuntimeConfiguration.GetRequiredJwtSettings(configuration));

        Assert.Contains("JwtSettings:SecretKey", exception.Message);
    }

    [Theory]
    [InlineData("too-short", "issuer", "audience", "7", "SecretKey")]
    [InlineData("01234567890123456789012345678901", "", "audience", "7", "Issuer")]
    [InlineData("01234567890123456789012345678901", "issuer", "", "7", "Audience")]
    [InlineData("01234567890123456789012345678901", "issuer", "audience", "0", "ExpiryInDays")]
    [InlineData("01234567890123456789012345678901", "issuer", "audience", "366", "ExpiryInDays")]
    public void Invalid_jwt_settings_are_rejected(
        string secretKey,
        string issuer,
        string audience,
        string expiryInDays,
        string invalidKey)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = secretKey,
            ["JwtSettings:Issuer"] = issuer,
            ["JwtSettings:Audience"] = audience,
            ["JwtSettings:ExpiryInDays"] = expiryInDays
        });

        var exception = Assert.Throws<InvalidOperationException>(
            () => RuntimeConfiguration.GetRequiredJwtSettings(configuration));

        Assert.Contains(invalidKey, exception.Message);
    }

    [Fact]
    public void Valid_jwt_settings_are_bound()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = "01234567890123456789012345678901",
            ["JwtSettings:Issuer"] = "restaurant-api",
            ["JwtSettings:Audience"] = "restaurant-client",
            ["JwtSettings:ExpiryInDays"] = "14"
        });

        var settings = RuntimeConfiguration.GetRequiredJwtSettings(configuration);

        Assert.Equal("01234567890123456789012345678901", settings.SecretKey);
        Assert.Equal("restaurant-api", settings.Issuer);
        Assert.Equal("restaurant-client", settings.Audience);
        Assert.Equal(14, settings.ExpiryInDays);
    }

    [Fact]
    public void Missing_database_connection_is_rejected()
    {
        var configuration = BuildConfiguration([]);

        var exception = Assert.Throws<InvalidOperationException>(
            () => RuntimeConfiguration.GetRequiredConnectionString(configuration));

        Assert.Contains("ConnectionStrings:ConnectionSql", exception.Message);
    }

    [Fact]
    public void External_database_connection_is_accepted()
    {
        const string connectionString =
            "Server=sql.example.test;Database=Restaurant;Integrated Security=True;TrustServerCertificate=True";
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:ConnectionSql"] = connectionString
        });

        var result = RuntimeConfiguration.GetRequiredConnectionString(configuration);

        Assert.Equal(connectionString, result);
    }

    private static IConfiguration BuildConfiguration(
        IEnumerable<KeyValuePair<string, string?>> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
}
