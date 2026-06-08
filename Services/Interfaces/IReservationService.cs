using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RestauranteAPI.DTOs;

namespace RestauranteAPI.Services.Interfaces
{
    public interface IReservationService
    {
        Task<IEnumerable<ReservationDto>> GetAllAsync();
        Task<ReservationDto?> GetByIdAsync(int id);
        Task<ReservationDto> CreateAsync(CreateReservationRequest request);
        Task<ReservationDto?> UpdateAsync(int id, CreateReservationRequest request);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<ReservationDto>> GetByClientAsync(int clientId);
        Task<IEnumerable<ReservationDto>> GetByDateAsync(DateOnly date);
        Task<ReservationDto?> UpdateStatusAsync(int id, int statusId);
    }
}
