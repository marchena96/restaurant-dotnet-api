namespace RestauranteAPI.Models;

public class Person
{
    public int PersonId { get; set; }
    public string? IdentificationNumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    public ClientProfile? ClientProfile { get; set; }
    public UserAccount? UserAccount { get; set; }
}
