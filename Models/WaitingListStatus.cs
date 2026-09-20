namespace RestauranteAPI.Models;

public class WaitingListStatus
{
    public int WaitingListStatusId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsTerminal { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public ICollection<WaitingListEntry> WaitingListEntries { get; set; } = new List<WaitingListEntry>();
}
