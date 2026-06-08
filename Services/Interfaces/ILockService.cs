using System.Collections.Generic;
using System.Threading.Tasks;
using RestauranteAPI.DTOs;

namespace RestauranteAPI.Services.Interfaces
{
    public interface ILockService
    {
        Task<IEnumerable<TableLockDto>> GetAllAsync();
        Task<TableLockDto?> GetByIdAsync(int id);
        Task<TableLockDto> CreateLockAsync(TableLockDto lockDto);
        Task<TableLockDto?> UpdateLockAsync(int id, TableLockDto lockDto);
        Task<bool> ReleaseLockAsync(int id);
    }
}
