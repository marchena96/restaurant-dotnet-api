using Microsoft.EntityFrameworkCore;
using RestauranteAPI.Data;

namespace RestauranteAPI.Hosting;

public static class MigrationMode
{
    public const string Argument = "--migrate";

    public static bool IsRequested(IEnumerable<string> arguments) =>
        arguments.Contains(Argument, StringComparer.Ordinal);

    public static string[] RemoveArgument(IEnumerable<string> arguments) =>
        arguments.Where(argument => !string.Equals(argument, Argument, StringComparison.Ordinal)).ToArray();

    public static async Task<int> RunAsync(
        MyAppDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await context.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migrations completed successfully.");
            return 0;
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "Database migration failed.");
            return 1;
        }
    }
}
