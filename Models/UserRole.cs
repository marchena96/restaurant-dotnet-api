namespace RestauranteAPI.Models;

public class UserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public int? AssignedByUserId { get; set; }

    public UserAccount User { get; set; } = null!;
    public Role Role { get; set; } = null!;
    public UserAccount? AssignedByUser { get; set; }
}
