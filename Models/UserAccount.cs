namespace RestauranteAPI.Models;

public class UserAccount
{
    public int UserId { get; set; }
    public int PersonId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public DateTime? PasswordChangedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    public Person Person { get; set; } = null!;
    public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();
    public ICollection<UserRole> AssignedRoles { get; set; } = new List<UserRole>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
