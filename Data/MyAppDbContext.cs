using Microsoft.EntityFrameworkCore;
using RestauranteAPI.Entidades;

namespace RestauranteAPI.Data
{
    public class MyAppDbContext : DbContext
    {
        public MyAppDbContext(DbContextOptions<MyAppDbContext> options) : base(options)
        {
        }

        public DbSet<Estado> Estados { get; set; } = null!;
        public DbSet<ListaEspera> ListaEspera { get; set; } = null!;
        public DbSet<Zona> Zonas { get; set; } = null!;
        public DbSet<Mesa> Mesas { get; set; } = null!;
        public DbSet<Cliente> Clientes { get; set; } = null!;
        public DbSet<Reserva> Reservas { get; set; } = null!;
        public DbSet<Bloqueo> Bloqueos { get; set; } = null!;
        public DbSet<Turno> Turnos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Mesa>()
                .HasOne<Zona>()
                .WithMany()
                .HasForeignKey(m => m.ZonaId);

            modelBuilder.Entity<Reserva>()
                .HasOne<Cliente>()
                .WithMany()
                .HasForeignKey(r => r.ClienteId);

            modelBuilder.Entity<ListaEspera>()
                .HasOne<Cliente>()
                .WithMany()
                .HasForeignKey(le => le.ClienteId);

            modelBuilder.Entity<Reserva>()
                .HasOne<Mesa>()
                .WithMany()
                .HasForeignKey(r => r.MesaId);

            modelBuilder.Entity<Bloqueo>()
                .HasOne<Mesa>()
                .WithMany()
                .HasForeignKey(b => b.MesaId);

            modelBuilder.Entity<Reserva>()
                .HasOne<Estado>()
                .WithMany()
                .HasForeignKey(r => r.EstadoId);

           
        }
    }
}