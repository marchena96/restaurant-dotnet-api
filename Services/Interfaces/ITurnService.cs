using System.Collections.Generic;
using System.Threading.Tasks;
using RestauranteAPI.DTOs;

namespace RestauranteAPI.Services.Interfaces
{
    public interface ITurnService
    {
        Task<IEnumerable<TurnDto>> GetAllAsync();
        Task<TurnDto?> GetByIdAsync(int id);
        Task<TurnDto> CreateAsync(TurnDto turnDto);
        Task<TurnDto?> UpdateAsync(int id, TurnDto turnDto);
        Task<bool> DeleteAsync(int id);
    }
}
