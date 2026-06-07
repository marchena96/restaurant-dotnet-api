using Microsoft.AspNetCore.Mvc;
using RestauranteAPI.DTOs;
using RestauranteAPI.Entidades;

namespace RestauranteAPI.Servicios.Interfaces
{
    public interface IBloqueoService
    {
        public List<BloqueoResponseDTO> GetAllBloqueos();
        
        public BloqueoResponseDTO ActualizarBloqueo(int bloqueoId, BloqueoResponseDTO bloqueoDTO);//cambiar por bloqueoDTO
        public BloqueoResponseDTO GetBloqueoById(int bloqueoId);
        

        public BloqueoResponseDTO BloquearMesa(BloqueoCreateDTO bloqueoDTO);

        void DesbloquearMesa(int bloqueoId);
    }
}
