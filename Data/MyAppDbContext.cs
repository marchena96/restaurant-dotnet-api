using Microsoft.EntityFrameworkCore;
using RestauranteAPI.Models;

namespace RestauranteAPI.Data
{
    public class MyAppDbContext : DbContext
    {
        public MyAppDbContext(DbContextOptions<MyAppDbContext> options) : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Reservation> Reservations { get; set; } = null!;
        public DbSet<WaitingListEntry> WaitingLists { get; set; } = null!;
        public DbSet<Table> Tables { get; set; } = null!;
        public DbSet<Zone> Zones { get; set; } = null!;
        public DbSet<Status> Statuses { get; set; } = null!;
        public DbSet<TableLock> TableLocks { get; set; } = null!;
        public DbSet<Turn> Turns { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Client Constraints & Indexes
            modelBuilder.Entity<Client>()
                .HasIndex(c => c.IdCard)
                .IsUnique();

            // User Constraints & Indexes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // 1:N relationships with DeleteBehavior.Restrict

            // Zone -> Table
            modelBuilder.Entity<Table>()
                .HasOne(t => t.Zone)
                .WithMany(z => z.Tables)
                .HasForeignKey(t => t.ZoneId)
                .OnDelete(DeleteBehavior.Restrict);

            // Client -> Reservation
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Client)
                .WithMany(c => c.Reservations)
                .HasForeignKey(r => r.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Client -> WaitingList
            modelBuilder.Entity<WaitingListEntry>()
                .HasOne(w => w.Client)
                .WithMany(c => c.WaitingListEntries)
                .HasForeignKey(w => w.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Table -> Reservation
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Table)
                .WithMany(t => t.Reservations)
                .HasForeignKey(r => r.TableId)
                .OnDelete(DeleteBehavior.Restrict);

            // Table -> TableLock
            modelBuilder.Entity<TableLock>()
                .HasOne(tl => tl.Table)
                .WithMany(t => t.TableLocks)
                .HasForeignKey(tl => tl.TableId)
                .OnDelete(DeleteBehavior.Restrict);

            // Status -> Reservation
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Status)
                .WithMany(s => s.Reservations)
                .HasForeignKey(r => r.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Turn -> Reservation
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Turn)
                .WithMany(t => t.Reservations)
                .HasForeignKey(r => r.TurnId)
                .OnDelete(DeleteBehavior.Restrict);

            // Data Seeding for Statuses in English
            modelBuilder.Entity<Status>().HasData(
                new Status { Id = 1, Name = "Active" },
                new Status { Id = 2, Name = "Pending" },
                new Status { Id = 3, Name = "Completed" },
                new Status { Id = 4, Name = "Cancelled" }
            );
        }
    }
}
