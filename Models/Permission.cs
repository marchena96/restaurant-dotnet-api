namespace RestauranteAPI.Models;

public class Permission
{
    public int PermissionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public ICollection<RolePermission> Roles { get; set; } = new List<RolePermission>();
}
