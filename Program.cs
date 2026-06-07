using Microsoft.EntityFrameworkCore;
using RestauranteAPI.Data;
using RestauranteAPI.Entidades;
using RestauranteAPI.Servicios.Implementaciones;
using RestauranteAPI.Servicios.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<MyAppDbContext>(options =>
    options.UseInMemoryDatabase("RestauranteDb"));

// Servicios
builder.Services.AddScoped<IMesaService, MesaService>();
builder.Services.AddScoped<IBloqueoService, BloqueoService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IListaEsperaService, ListaEsperaService>();
builder.Services.AddScoped<ITurnoService, TurnoService>();
builder.Services.AddScoped<IEstadoService, EstadoService>();
builder.Services.AddScoped<IReservaService, ReservaService>();
builder.Services.AddScoped<IZonaService, ZonaService>();

builder.Services.AddControllers();

var app = builder.Build();

// LLAMADA AL SeedData
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MyAppDbContext>();
    context.Database.EnsureCreated();
    SeedData.Initialize(context);
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();