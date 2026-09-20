using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestauranteAPI.Configuration;
using RestauranteAPI.Data;
using RestauranteAPI.Hosting;
using RestauranteAPI.Middleware;
using RestauranteAPI.Services.Implementations;
using RestauranteAPI.Services.Interfaces;

var migrationMode = MigrationMode.IsRequested(args);
var builder = WebApplication.CreateBuilder(MigrationMode.RemoveArgument(args));

var connectionString = RuntimeConfiguration.GetRequiredConnectionString(builder.Configuration);
builder.Services.AddDbContext<MyAppDbContext>(options =>
    options.UseSqlServer(connectionString));

if (migrationMode)
{
    await using var migrationApp = builder.Build();
    await using var scope = migrationApp.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<MyAppDbContext>();
    return await MigrationMode.RunAsync(context, migrationApp.Logger);
}

var jwtSettings = RuntimeConfiguration.GetRequiredJwtSettings(builder.Configuration);
builder.Services.AddSingleton(jwtSettings);

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IZoneService, ZoneService>();
builder.Services.AddScoped<IStatusService, StatusService>();
builder.Services.AddScoped<ITurnService, TurnService>();
builder.Services.AddScoped<ILockService, LockService>();
builder.Services.AddScoped<IWaitingListService, WaitingListService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MyAppDbContext>();
    SeedData.Initialize(context);
}

app.UseGlobalExceptionMiddleware();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
return 0;
