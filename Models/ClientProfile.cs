namespace RestauranteAPI.Models;

public class ClientProfile
{
    public int ClientId { get; set; }
    public int PersonId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CustomerSinceUtc { get; set; }
    public string? Notes { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Person Person { get; set; } = null!;
}
