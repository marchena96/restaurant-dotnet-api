using Microsoft.EntityFrameworkCore;
using RestauranteAPI.Data;

namespace RestauranteAPI.Tests.Database;

public sealed class DbContextModelTests
{
    [Fact]
    public void Current_sql_server_model_can_be_built()
    {
        var options = new DbContextOptionsBuilder<MyAppDbContext>()
            .UseSqlServer("Server=localhost;Database=ModelOnly;User Id=unused;Password=unused;TrustServerCertificate=True")
            .Options;

        using var context = new MyAppDbContext(options);

        Assert.NotEmpty(context.Model.GetEntityTypes());
    }
}
