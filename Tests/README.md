# Test Foundation

## Prerequisites

- .NET 10 SDK
- SQL Server reachable with credentials allowed to create and drop test databases for integration tests

Integration tests never use a developer SQL Express instance implicitly. Supply the server-level connection externally:

```powershell
$env:RESTAURANT_TEST_SQLSERVER_CONNECTION_STRING = "Server=localhost,1433;Database=master;User Id=sa;Password=<password>;TrustServerCertificate=True"
```

Each integration fixture creates a uniquely named `RestaurantTests_<guid>` database, applies existing EF Core migrations, and drops the database after the test class. The fixture does not use `EnsureCreated()`.

## Commands

Run from repository root.

Restore:

```powershell
dotnet restore RestauranteAPI.slnx
```

Build:

```powershell
dotnet build RestauranteAPI.slnx --no-restore
```

Unit tests only:

```powershell
dotnet test Tests/RestauranteAPI.Tests/RestauranteAPI.Tests.csproj --no-build --filter "Category!=Integration"
```

SQL Server integration tests:

```powershell
dotnet test Tests/RestauranteAPI.Tests/RestauranteAPI.Tests.csproj --no-build --filter "Category=Integration"
```

All tests:

```powershell
dotnet test RestauranteAPI.slnx --no-build
```
