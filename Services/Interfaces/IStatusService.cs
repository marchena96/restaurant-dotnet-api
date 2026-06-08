using System.Collections.Generic;
using System.Threading.Tasks;
using RestauranteAPI.DTOs;

namespace RestauranteAPI.Services.Interfaces
{
    public interface IStatusService
    {
        Task<IEnumerable<StatusDto>> GetAllAsync();
        Task<StatusDto?> GetByIdAsync(int id);
        Task<StatusDto> CreateAsync(StatusDto statusDto);
        Task<StatusDto?> UpdateAsync(int id, StatusDto statusDto);
        Task<bool> DeleteAsync(int id);
    }
}
