namespace RestauranteAPI.Models;

public class AuditLog
{
    public long AuditLogId { get; set; }
    public int? UserId { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public Guid? CorrelationId { get; set; }
    public string? IpAddress { get; set; }
    public string? Details { get; set; }

    public UserAccount? User { get; set; }
}
