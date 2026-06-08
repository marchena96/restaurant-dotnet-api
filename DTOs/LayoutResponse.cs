using System.Collections.Generic;

namespace RestauranteAPI.DTOs
{
    public class LayoutResponse
    {
        public IEnumerable<ZoneSummaryDto> Zones { get; set; } = new List<ZoneSummaryDto>();
        public IEnumerable<TableDto> Tables { get; set; } = new List<TableDto>();
    }
}
