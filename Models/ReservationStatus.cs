namespace RestauranteAPI.Models;

public class ReservationStatus
{
    public int ReservationStatusId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool BlocksAvailability { get; set; }
    public bool IsTerminal { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
