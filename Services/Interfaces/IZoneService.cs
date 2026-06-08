using System.Collections.Generic;
using System.Threading.Tasks;
using RestauranteAPI.DTOs;

namespace RestauranteAPI.Services.Interfaces
{
    public interface IZoneService
    {
        Task<IEnumerable<ZoneDto>> GetAllAsync();
        Task<ZoneDto?> GetByIdAsync(int id);
        Task<ZoneDto> CreateAsync(ZoneDto zoneDto);
        Task<ZoneDto?> UpdateAsync(int id, ZoneDto zoneDto);
        Task<bool> DeleteAsync(int id);
    }
}
