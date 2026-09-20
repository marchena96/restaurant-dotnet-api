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
        public DbSet<Person> People { get; set; } = null!;
        public DbSet<ClientProfile> ClientProfiles { get; set; } = null!;
        public DbSet<UserAccount> UserAccounts { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Permission> Permissions { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<ReservationStatus> ReservationStatuses { get; set; } = null!;
        public DbSet<WaitingListStatus> WaitingListStatuses { get; set; } = null!;

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

            modelBuilder.Entity<Person>(entity =>
            {
                entity.HasKey(person => person.PersonId);
                entity.Property(person => person.IdentificationNumber).HasMaxLength(50);
                entity.Property(person => person.FirstName).HasMaxLength(100).IsRequired();
                entity.Property(person => person.LastName).HasMaxLength(100).IsRequired();
                entity.Property(person => person.Email).HasMaxLength(254);
                entity.Property(person => person.PhoneNumber).HasMaxLength(30);
                entity.Property(person => person.IsActive).HasDefaultValue(true);
                entity.Property(person => person.CreatedAtUtc).HasColumnType("datetime2(3)");
                entity.Property(person => person.UpdatedAtUtc).HasColumnType("datetime2(3)");
                entity.Property(person => person.RowVersion).IsRowVersion();
                entity.HasIndex(person => person.IdentificationNumber)
                    .IsUnique()
                    .HasFilter("[IdentificationNumber] IS NOT NULL");
            });

            modelBuilder.Entity<ClientProfile>(entity =>
            {
                entity.HasKey(profile => profile.ClientId);
                entity.Property(profile => profile.CustomerSinceUtc).HasColumnType("datetime2(3)");
                entity.Property(profile => profile.Notes).HasMaxLength(500);
                entity.Property(profile => profile.UpdatedAtUtc).HasColumnType("datetime2(3)");
                entity.HasIndex(profile => profile.PersonId).IsUnique();
                entity.HasOne(profile => profile.Person)
                    .WithOne(person => person.ClientProfile)
                    .HasForeignKey<ClientProfile>(profile => profile.PersonId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserAccount>(entity =>
            {
                entity.HasKey(account => account.UserId);
                entity.Property(account => account.Username).HasMaxLength(100).IsRequired();
                entity.Property(account => account.PasswordHash).HasMaxLength(255).IsRequired();
                entity.Property(account => account.FailedLoginAttempts).HasDefaultValue(0);
                entity.Property(account => account.LockedUntilUtc).HasColumnType("datetime2(3)");
                entity.Property(account => account.PasswordChangedAtUtc).HasColumnType("datetime2(3)");
                entity.Property(account => account.CreatedAtUtc).HasColumnType("datetime2(3)");
                entity.Property(account => account.UpdatedAtUtc).HasColumnType("datetime2(3)");
                entity.Property(account => account.RowVersion).IsRowVersion();
                entity.HasIndex(account => account.PersonId).IsUnique();
                entity.HasIndex(account => account.Username).IsUnique();
                entity.ToTable(table => table.HasCheckConstraint(
                    "CK_UserAccounts_FailedLoginAttempts_NonNegative",
                    "[FailedLoginAttempts] >= 0"));
                entity.HasOne(account => account.Person)
                    .WithOne(person => person.UserAccount)
                    .HasForeignKey<UserAccount>(account => account.PersonId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(role => role.RoleId);
                entity.Property(role => role.Code).HasMaxLength(80).IsRequired();
                entity.Property(role => role.Name).HasMaxLength(100).IsRequired();
                entity.Property(role => role.Description).HasMaxLength(300);
                entity.Property(role => role.CreatedAtUtc).HasColumnType("datetime2(3)");
                entity.Property(role => role.UpdatedAtUtc).HasColumnType("datetime2(3)");
                entity.HasIndex(role => role.Code).IsUnique();
            });

            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(permission => permission.PermissionId);
                entity.Property(permission => permission.Code).HasMaxLength(120).IsRequired();
                entity.Property(permission => permission.Name).HasMaxLength(120).IsRequired();
                entity.Property(permission => permission.Description).HasMaxLength(300);
                entity.HasIndex(permission => permission.Code).IsUnique();
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(userRole => new { userRole.UserId, userRole.RoleId });
                entity.Property(userRole => userRole.AssignedAtUtc).HasColumnType("datetime2(3)");
                entity.HasOne(userRole => userRole.User)
                    .WithMany(account => account.Roles)
                    .HasForeignKey(userRole => userRole.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(userRole => userRole.Role)
                    .WithMany(role => role.Users)
                    .HasForeignKey(userRole => userRole.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(userRole => userRole.AssignedByUser)
                    .WithMany(account => account.AssignedRoles)
                    .HasForeignKey(userRole => userRole.AssignedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId });
                entity.HasOne(rolePermission => rolePermission.Role)
                    .WithMany(role => role.Permissions)
                    .HasForeignKey(rolePermission => rolePermission.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(rolePermission => rolePermission.Permission)
                    .WithMany(permission => permission.Roles)
                    .HasForeignKey(rolePermission => rolePermission.PermissionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(log => log.AuditLogId);
                entity.Property(log => log.AuditLogId).UseIdentityColumn();
                entity.Property(log => log.ActionCode).HasMaxLength(120).IsRequired();
                entity.Property(log => log.EntityName).HasMaxLength(100).IsRequired();
                entity.Property(log => log.EntityId).HasMaxLength(100);
                entity.Property(log => log.OccurredAtUtc).HasColumnType("datetime2(3)");
                entity.Property(log => log.IpAddress).HasMaxLength(45).IsUnicode(false);
                entity.Property(log => log.Details).HasColumnType("nvarchar(max)");
                entity.HasIndex(log => new { log.UserId, log.OccurredAtUtc });
                entity.HasIndex(log => new { log.EntityName, log.EntityId, log.OccurredAtUtc });
                entity.HasOne(log => log.User)
                    .WithMany(account => account.AuditLogs)
                    .HasForeignKey(log => log.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ReservationStatus>(entity =>
            {
                entity.ToTable("ReservationStatus");
                entity.HasKey(status => status.ReservationStatusId);
                entity.Property(status => status.Code).HasMaxLength(50).IsRequired();
                entity.Property(status => status.Name).HasMaxLength(100).IsRequired();
                entity.HasIndex(status => status.Code).IsUnique();
                entity.ToTable(table => table.HasCheckConstraint(
                    "CK_ReservationStatus_SortOrder", "[SortOrder] >= 0"));
            });

            modelBuilder.Entity<WaitingListStatus>(entity =>
            {
                entity.ToTable("WaitingListStatus");
                entity.HasKey(status => status.WaitingListStatusId);
                entity.Property(status => status.Code).HasMaxLength(50).IsRequired();
                entity.Property(status => status.Name).HasMaxLength(100).IsRequired();
                entity.HasIndex(status => status.Code).IsUnique();
                entity.ToTable(table => table.HasCheckConstraint(
                    "CK_WaitingListStatus_SortOrder", "[SortOrder] >= 0"));
            });

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

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.ReservationStatus)
                .WithMany(s => s.Reservations)
                .HasForeignKey(r => r.ReservationStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WaitingListEntry>()
                .HasOne(w => w.WaitingListStatus)
                .WithMany(s => s.WaitingListEntries)
                .HasForeignKey(w => w.WaitingListStatusId)
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
