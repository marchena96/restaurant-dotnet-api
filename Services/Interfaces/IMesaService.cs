using RestauranteAPI.DTOs;
using RestauranteAPI.Entidades;

namespace RestauranteAPI.Servicios.Interfaces
{
    public interface IMesaService
    {
        public List<MesaResponseDTO> GetAllMesas();
        public MesaResponseDTO GetMesaById(int mesaId);
        public MesaResponseDTO CrearMesa(MesaCreateDTO mesaDTO);
        public MesaResponseDTO ActualizarMesa(int mesaId, MesaCreateDTO mesaDTO);
        public void EliminarMesa(int mesaId);
        public List<MesaResponseDTO> GetMesasDisponibles(DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin);
        public bool ValidarDisponibilidadMesa(int mesaId, DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin);
        
        public MesaResponseDTO BuscarMesaPorNumero(int numeroMesa);

        public List<MesaResponseDTO> BuscarMesaPorCapacidad(int capacidad);

    }
}