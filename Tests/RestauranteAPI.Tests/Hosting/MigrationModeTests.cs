using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RestauranteAPI.Data;
using RestauranteAPI.Hosting;

namespace RestauranteAPI.Tests.Hosting;

public sealed class MigrationModeTests
{
    [Fact]
    public void Migration_argument_is_detected_and_removed_from_host_arguments()
    {
        string[] arguments = ["--environment", "Development", MigrationMode.Argument];

        Assert.True(MigrationMode.IsRequested(arguments));
        Assert.Equal(["--environment", "Development"], MigrationMode.RemoveArgument(arguments));
    }

    [Fact]
    public async Task Migration_failure_returns_nonzero_exit_code()
    {
        var options = new DbContextOptionsBuilder<MyAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new MyAppDbContext(options);

        var exitCode = await MigrationMode.RunAsync(
            context,
            NullLogger.Instance);

        Assert.Equal(1, exitCode);
    }
}
