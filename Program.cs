using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestauranteAPI.Data;
using RestauranteAPI.Middleware;
using RestauranteAPI.Services.Implementations;
using RestauranteAPI.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 1. Carga de configuración de JWT alineada con appsettings.json
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
string jwtSecret = jwtSettings["SecretKey"] ?? "SuperSecretDefaultKeyMustBeLongEnough1234567890!";

// 2. Configuración del Acceso a Datos (SQL Server)
var connectionString = builder.Configuration.GetConnectionString("ConnectionSql");
builder.Services.AddDbContext<MyAppDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Inyección de Dependencias (Capa de Servicios en Inglés)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IZoneService, ZoneService>();
builder.Services.AddScoped<IStatusService, StatusService>();
builder.Services.AddScoped<ITurnService, TurnService>();
builder.Services.AddScoped<ILockService, LockService>();
builder.Services.AddScoped<IWaitingListService, WaitingListService>();

// 4. Configuración de Autenticación JWT mediante Bearer header
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };

        // El token se lee automaticamente del header Authorization: Bearer <token>
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// 5. Configuración de CORS con soporte explícito para Credenciales (Cookies)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Permite el intercambio de cookies HttpOnly
    });
});

var app = builder.Build();

// 6. Inicialización de la Base de Datos y Sembrado de Datos (Seed Data)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MyAppDbContext>();
    context.Database.Migrate();
    SeedData.Initialize(context);
}

// 7. Pipeline de Middlewares (El orden estricto garantiza la seguridad)
app.UseGlobalExceptionMiddleware();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


// ORIGINAL CODE 
// using System.Text;
// using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.IdentityModel.Tokens;
// using RestauranteAPI.Data;
// using RestauranteAPI.Services.Implementations;
// using RestauranteAPI.Services.Interfaces;

// var builder = WebApplication.CreateBuilder(args);

// string jwtSecret = builder.Configuration["Jwt:Secret"] ?? "SuperSecretDefaultKeyMustBeLongEnough1234567890!";

// builder.Services.AddDbContext<MyAppDbContext>(options =>
//     options.UseInMemoryDatabase("RestauranteDb"));

// builder.Services.AddScoped<IAuthService, AuthService>();
// builder.Services.AddScoped<IClientService, ClientService>();
// builder.Services.AddScoped<IReservationService, ReservationService>();
// builder.Services.AddScoped<ITableService, TableService>();
// builder.Services.AddScoped<IZoneService, ZoneService>();
// builder.Services.AddScoped<IStatusService, StatusService>();
// builder.Services.AddScoped<ITurnService, TurnService>();
// builder.Services.AddScoped<ILockService, LockService>();
// builder.Services.AddScoped<IWaitingListService, WaitingListService>();

// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(options =>
//     {
//         options.TokenValidationParameters = new TokenValidationParameters
//         {
//             ValidateIssuer = false,
//             ValidateAudience = false,
//             ValidateLifetime = true,
//             ValidateIssuerSigningKey = true,
//             IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
//         };

//         options.Events = new JwtBearerEvents
//         {
//             OnMessageReceived = context =>
//             {
//                 string? token = context.Request.Cookies["jwt"];
//                 if (!string.IsNullOrEmpty(token))
//                 {
//                     context.Token = token;
//                 }
//                 return Task.CompletedTask;
//             }
//         };
//     });

// builder.Services.AddAuthorization();
// builder.Services.AddControllers();

// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowFrontend", policy =>
//     {
//         policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
//               .AllowAnyHeader()
//               .AllowAnyMethod()
//               .AllowCredentials();
//     });
// });

// var app = builder.Build();

// using (var scope = app.Services.CreateScope())
// {
//     var context = scope.ServiceProvider.GetRequiredService<MyAppDbContext>();
//     context.Database.EnsureCreated();
//     SeedData.Initialize(context);
// }

// app.UseCors("AllowFrontend");
// app.UseAuthentication();
// app.UseAuthorization();
// app.MapControllers();

// app.Run();
