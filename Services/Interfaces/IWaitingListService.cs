using System.Collections.Generic;
using System.Threading.Tasks;
using RestauranteAPI.DTOs;

namespace RestauranteAPI.Services.Interfaces
{
    public interface IWaitingListService
    {
        Task<IEnumerable<WaitingListDto>> GetAllAsync();
        Task<WaitingListDto?> GetByIdAsync(int id);
        Task<WaitingListDto> CreateAsync(WaitingListDto waitingListDto);
        Task<WaitingListDto?> UpdateAsync(int id, WaitingListDto waitingListDto);
        Task<bool> DeleteAsync(int id);
        Task<bool> PromoteToReservationAsync(int waitingListId, int tableId);
    }
}
